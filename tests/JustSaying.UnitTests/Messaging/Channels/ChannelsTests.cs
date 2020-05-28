using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Channels.Dispatch;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable PossibleNullReferenceException
#pragma warning disable 4014

namespace JustSaying.UnitTests.Messaging.Channels
{
    public class ChannelsTests
    {
        private ILoggerFactory LoggerFactory { get; }
        private IMessageMonitor MessageMonitor { get; }

        public ChannelsTests(ITestOutputHelper testOutputHelper)
        {
            LoggerFactory = testOutputHelper.ToLoggerFactory();
            MessageMonitor = new LoggingMonitor(LoggerFactory.CreateLogger<IMessageMonitor>());
        }

        private static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMilliseconds(100);

        [Fact]
        public async Task QueueCanBeAssignedToOnePump()
        {
            var sqsQueue = TestQueue();
            var buffer = CreateMessageReceiveBuffer(sqsQueue);
            IMessageDispatcher dispatcher = new FakeDispatcher();
            IMultiplexerSubscriber multiplexerSubscriber = CreateSubscriber(dispatcher);
            IMultiplexer multiplexer = CreateMultiplexer();

            multiplexer.ReadFrom(buffer.Reader);
            multiplexerSubscriber.Subscribe(multiplexer.GetMessagesAsync());

            // need to start the multiplexer before calling Start

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            var multiplexerCompletion = multiplexer.Run(cts.Token);
            var consumer1Completion = multiplexerSubscriber.Run(cts.Token);
            var buffer1Completion = buffer.Run(cts.Token);

            await multiplexerCompletion;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer1Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => buffer1Completion);
        }

        [Fact]
        public async Task QueueCanBeAssignedToMultiplePumps()
        {
            var sqsQueue = TestQueue();
            var buffer = CreateMessageReceiveBuffer(sqsQueue);

            IMessageDispatcher dispatcher = new FakeDispatcher();
            IMultiplexerSubscriber consumer1 = CreateSubscriber(dispatcher);
            IMultiplexerSubscriber consumer2 = CreateSubscriber(dispatcher);

            IMultiplexer multiplexer = CreateMultiplexer();

            multiplexer.ReadFrom(buffer.Reader);
            consumer1.Subscribe(multiplexer.GetMessagesAsync());
            consumer2.Subscribe(multiplexer.GetMessagesAsync());

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            var multiplexerCompletion = multiplexer.Run(cts.Token);

            // consumers
            var consumer1Completion = consumer1.Run(cts.Token);
            var consumer2Completion = consumer2.Run(cts.Token);

            var buffer1Completion = buffer.Run(cts.Token);

            await multiplexerCompletion;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer1Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer2Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => buffer1Completion);
        }

        [Fact]
        public async Task MultipleQueuesCanBeAssignedToOnePump()
        {
            var sqsQueue1 = TestQueue();
            var sqsQueue2 = TestQueue();
            var buffer1 = CreateMessageReceiveBuffer(sqsQueue1);
            var buffer2 = CreateMessageReceiveBuffer(sqsQueue2);

            IMessageDispatcher dispatcher = new FakeDispatcher();
            IMultiplexerSubscriber multiplexerSubscriber = CreateSubscriber(dispatcher);

            IMultiplexer multiplexer = CreateMultiplexer();

            multiplexer.ReadFrom(buffer1.Reader);
            multiplexer.ReadFrom(buffer2.Reader);

            multiplexerSubscriber.Subscribe(multiplexer.GetMessagesAsync());

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            var multiplexerCompletion = multiplexer.Run(cts.Token);

            // consumers
            var consumer1Completion = multiplexerSubscriber.Run(cts.Token);

            var buffer1Completion = buffer1.Run(cts.Token);
            var buffer2Completion = buffer2.Run(cts.Token);

            await multiplexerCompletion;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => buffer1Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => buffer2Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer1Completion);
        }

        [Fact]
        public async Task MultipleQueuesCanBeAssignedToMultiplePumps()
        {
            var sqsQueue1 = TestQueue();
            var sqsQueue2 = TestQueue();
            var buffer1 = CreateMessageReceiveBuffer(sqsQueue1);
            var buffer2 = CreateMessageReceiveBuffer(sqsQueue2);

            // using 2 dispatchers for logging, they should be the same/stateless
            IMessageDispatcher dispatcher1 = new FakeDispatcher();
            IMessageDispatcher dispatcher2 = new FakeDispatcher();
            IMultiplexerSubscriber consumer1 = CreateSubscriber(dispatcher1);
            IMultiplexerSubscriber consumer2 = CreateSubscriber(dispatcher2);

            IMultiplexer multiplexer = CreateMultiplexer();

            multiplexer.ReadFrom(buffer1.Reader);
            multiplexer.ReadFrom(buffer2.Reader);

            consumer1.Subscribe(multiplexer.GetMessagesAsync());
            consumer2.Subscribe(multiplexer.GetMessagesAsync());

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            var multiplexerCompletion = multiplexer.Run(cts.Token);

            // consumers
            var consumer1Completion = consumer1.Run(cts.Token);
            var consumer2Completion = consumer2.Run(cts.Token);

            var buffer1Completion = buffer1.Run(cts.Token);
            var buffer2Completion = buffer2.Run(cts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => buffer1Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => buffer2Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer1Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer2Completion);
            await multiplexerCompletion;
        }

        [Fact]
        public async Task Subscriber_Not_Started_No_Buffer_Filled_Then_No_More_Messages_Requested()
        {
            // Arrange
            int messagesFromQueue = 0;
            int messagesDispatched = 0;
            int bufferSize = 5;
            int channelCapacity = 10;

            // plus one "in flight" between buffer and multiplexer
            int expectedReceiveFromQueueCount = bufferSize + channelCapacity + 1;

            var sqsQueue = TestQueue(() => Interlocked.Increment(ref messagesFromQueue));
            IMessageReceiveBuffer buffer = CreateMessageReceiveBuffer(sqsQueue, bufferSize);
            IMessageDispatcher dispatcher = new FakeDispatcher(() => Interlocked.Increment(ref messagesDispatched));
            IMultiplexerSubscriber multiplexerSubscriber = CreateSubscriber(dispatcher);
            IMultiplexer multiplexer = CreateMultiplexer(channelCapacity);

            multiplexer.ReadFrom(buffer.Reader);
            multiplexerSubscriber.Subscribe(multiplexer.GetMessagesAsync());

            // need to start the multiplexer before calling Messages

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(1));

            // Act and Assert
            var multiplexerCompletion = multiplexer.Run(cts.Token);
            var bufferCompletion = buffer.Run(cts.Token);

            await multiplexerCompletion;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bufferCompletion);

            messagesFromQueue.ShouldBe(expectedReceiveFromQueueCount);
            messagesDispatched.ShouldBe(0);

            // Starting the consumer after the token is cancelled will not dispatch messages
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => multiplexerSubscriber.Run(cts.Token));

            messagesFromQueue.ShouldBe(expectedReceiveFromQueueCount);
            messagesDispatched.ShouldBe(0);
        }

        [Fact]
        public async Task Can_Be_Set_Up_Using_SubscriptionBus()
        {
            var sqsQueue1 = TestQueue();
            var sqsQueue2 = TestQueue();
            var sqsQueue3 = TestQueue();

            var queues = new List<ISqsQueue> { sqsQueue1, sqsQueue2, sqsQueue3 };
            IMessageDispatcher dispatcher = new FakeDispatcher();
            var bus = CreateSubscriptionGroup(queues, dispatcher);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bus.Run(cts.Token));
        }

        [Fact]
        public async Task Sqs_Queue_Is_Not_Polled_After_Cancellation()
        {
            var cts = new CancellationTokenSource();

            int callCountBeforeCancelled = 0;
            int callCountAfterCancelled = 0;
            ISqsQueue sqsQueue = TestQueue(() =>
            {
                if (cts.Token.IsCancellationRequested)
                {
                    callCountAfterCancelled++;
                }
                else
                {
                    callCountBeforeCancelled++;
                }
            });

            IMessageDispatcher dispatcher = new FakeDispatcher();
            var bus = CreateSubscriptionGroup(new[] { sqsQueue }, dispatcher);

            var runTask = bus.Run(cts.Token);

            cts.CancelAfter(TimeoutPeriod);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

            callCountBeforeCancelled.ShouldBeGreaterThan(0);
            callCountAfterCancelled.ShouldBe(0);
        }

        [Fact]
        public async Task Messages_Not_Dispatched_After_Cancellation()
        {
            var cts = new CancellationTokenSource();

            int dispatchedBeforeCancelled = 0;
            int dispatchedAfterCancelled = 0;

            ISqsQueue sqsQueue = TestQueue();
            IMessageDispatcher dispatcher = new FakeDispatcher(() =>
            {
                if (cts.Token.IsCancellationRequested)
                {
                    dispatchedAfterCancelled++;
                }
                else
                {
                    dispatchedBeforeCancelled++;
                }
            });

            var bus = CreateSubscriptionGroup(new[] { sqsQueue }, dispatcher);

            var runTask = bus.Run(cts.Token);

            cts.CancelAfter(TimeoutPeriod);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await runTask);

            dispatchedBeforeCancelled.ShouldBeGreaterThan(0);
            dispatchedAfterCancelled.ShouldBe(0);
        }

        [Fact]
        public void SubscriptionGroup_StartingTwice_ShouldReturnSameCompletionTask()
        {
            var queue = TestQueue();
            var dispatcher = new FakeDispatcher();
            var bus = CreateSubscriptionGroup(new[] { queue }, dispatcher);

            var cts = new CancellationTokenSource(TimeoutPeriod);

            var task1 = bus.Run(cts.Token);
            var task2 = bus.Run(cts.Token);

            Assert.True(ReferenceEquals(task1, task2));
        }

        private static ISqsQueue TestQueue(Action spy = null)
        {
            IList<Message> GetMessages()
            {
                spy?.Invoke();
                return new List<Message>
                {
                    new TestMessage(),
                };
            }

            ISqsQueue sqsQueueMock = Substitute.For<ISqsQueue>();
            sqsQueueMock
                .GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(_ => GetMessages());

            return sqsQueueMock;
        }

        private IMessageReceiveBuffer CreateMessageReceiveBuffer(
            ISqsQueue sqsQueue,
            int bufferSize = 10)
        {
            return new MessageReceiveBuffer(
                10,
                bufferSize,
                TimeSpan.FromSeconds(1),
                sqsQueue,
                new DelegateMiddleware<GetMessagesContext, IList<Message>>(),
                Substitute.For<IMessageMonitor>(),
                LoggerFactory.CreateLogger<MessageReceiveBuffer>());
        }

        private static IMultiplexerSubscriber CreateSubscriber(IMessageDispatcher dispatcher)
        {
            return new MultiplexerSubscriber(dispatcher);
        }

        private ISubscriptionGroupCollection CreateSubscriptionGroup(
            IList<ISqsQueue> queues,
            IMessageDispatcher dispatcher)
        {
            var defaults = new SubscriptionConfigBuilder();

            var settings = new Dictionary<string, SubscriptionGroupConfigBuilder>
            {
                { "test",  new SubscriptionGroupConfigBuilder("test").AddQueues(queues) },
            };

            var consumerGroupFactory = new SubscriptionGroupFactory(
                dispatcher,
                MessageMonitor,
                LoggerFactory);

            return consumerGroupFactory.Create(defaults, settings);
        }

        private IMultiplexer CreateMultiplexer(int channelCapacity = 100)
        {
            return new MergingMultiplexer(
                channelCapacity,
                LoggerFactory.CreateLogger<MergingMultiplexer>());
        }

        private class TestMessage : Message
        {
            public override string ToString()
            {
                return Body;
            }
        }

        public class TestException : Exception
        {
            public TestException(string message) : base(message)
            {
            }

            public TestException(string message, Exception innerException) : base(message, innerException)
            {
            }

            public TestException()
            {
            }
        }
    }
}
