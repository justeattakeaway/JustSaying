using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Monitoring;
using JustSaying.Messaging.Policies;
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
        private readonly ITestOutputHelper _testOutputHelper;

        public ChannelsTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMilliseconds(100);

        [Fact]
        public async Task QueueCanBeAssignedToOnePump()
        {
            var sqsQueue = TestQueue();
            var buffer = CreateMessageReceiveBuffer(sqsQueue);
            IMessageDispatcher dispatcher = TestDispatcher();
            IChannelConsumer consumer = CreateChannelConsumer(dispatcher);
            IMultiplexer multiplexer = CreateMultiplexer();

            multiplexer.ReadFrom(buffer.Reader);
            consumer.ConsumeFrom(multiplexer.Messages());

            // need to start the multiplexer before calling Start

            // consumer
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            var multiplexerCompletion = multiplexer.Run(cts.Token);
            var consumer1Completion = consumer.Run(cts.Token);
            var buffer1Completion = buffer.Run(cts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => multiplexerCompletion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer1Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => buffer1Completion);
        }

        [Fact]
        public async Task QueueCanBeAssignedToMultiplePumps()
        {
            var sqsQueue = TestQueue();
            var buffer = CreateMessageReceiveBuffer(sqsQueue);

            IMessageDispatcher dispatcher = TestDispatcher();
            IChannelConsumer consumer1 = CreateChannelConsumer(dispatcher);
            IChannelConsumer consumer2 = CreateChannelConsumer(dispatcher);

            IMultiplexer multiplexer = CreateMultiplexer();

            multiplexer.ReadFrom(buffer.Reader);
            consumer1.ConsumeFrom(multiplexer.Messages());
            consumer2.ConsumeFrom(multiplexer.Messages());

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            var multiplexerCompletion = multiplexer.Run(cts.Token);

            // consumers
            var consumer1Completion = consumer1.Run(cts.Token);
            var consumer2Completion = consumer2.Run(cts.Token);

            var buffer1Completion = buffer.Run(cts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => multiplexerCompletion);
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

            IMessageDispatcher dispatcher = TestDispatcher();
            IChannelConsumer consumer = CreateChannelConsumer(dispatcher);

            IMultiplexer multiplexer = CreateMultiplexer();

            multiplexer.ReadFrom(buffer1.Reader);
            multiplexer.ReadFrom(buffer2.Reader);

            consumer.ConsumeFrom(multiplexer.Messages());

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            var multiplexerCompletion = multiplexer.Run(cts.Token);

            // consumers
            var consumer1Completion = consumer.Run(cts.Token);

            var buffer1Completion = buffer1.Run(cts.Token);
            var buffer2Completion = buffer2.Run(cts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => multiplexerCompletion);
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
            IMessageDispatcher dispatcher1 = TestDispatcher();
            IMessageDispatcher dispatcher2 = TestDispatcher();
            IChannelConsumer consumer1 = CreateChannelConsumer(dispatcher1);
            IChannelConsumer consumer2 = CreateChannelConsumer(dispatcher2);

            IMultiplexer multiplexer = CreateMultiplexer();

            multiplexer.ReadFrom(buffer1.Reader);
            multiplexer.ReadFrom(buffer2.Reader);

            consumer1.ConsumeFrom(multiplexer.Messages());
            consumer2.ConsumeFrom(multiplexer.Messages());

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
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => multiplexerCompletion);

        }

        [Fact]
        public async Task All_Messages_Are_Processed()
        {
            int messagesFromQueue = 0;
            int messagesDispatched = 0;
            var sqsQueue = TestQueue(() => messagesFromQueue++);

            IMessageReceiveBuffer buffer = CreateMessageReceiveBuffer(sqsQueue);
            IMessageDispatcher dispatcher = TestDispatcher(() => messagesDispatched++);
            IChannelConsumer consumer = CreateChannelConsumer(dispatcher);
            IMultiplexer multiplexer = CreateMultiplexer();

            multiplexer.ReadFrom(buffer.Reader);
            consumer.ConsumeFrom(multiplexer.Messages());

            // need to start the multiplexer before calling Messages

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            var multiplexerCompletion = multiplexer.Run(cts.Token);

            // consumer
            var consumer1Completion = consumer.Run(cts.Token);
            var buffer1Completion = buffer.Run(cts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => buffer1Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer1Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => multiplexerCompletion);

            messagesDispatched.ShouldBe(messagesFromQueue);
        }

        [Fact]
        public async Task Can_Be_Set_Up_Using_ConsumerBus()
        {
            var sqsQueue1 = TestQueue();
            var sqsQueue2 = TestQueue();
            var sqsQueue3 = TestQueue();

            var queues = new List<ISqsQueue> { sqsQueue1, sqsQueue2, sqsQueue3 };
            IMessageDispatcher dispatcher = TestDispatcher();
            var bus = CreateConsumerBus(queues, dispatcher);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bus.Run(cts.Token));
        }

        [Fact]
        public async Task On_Cancellation_All_Downloaded_Messages_Should_Be_Processed()
        {
            int messagesSent = 0;
            int messagesDispatched = 0;

            var sqsQueue1 = TestQueue(() => Interlocked.Increment(ref messagesSent));
            var sqsQueue2 = TestQueue(() => Interlocked.Increment(ref messagesSent));
            var sqsQueue3 = TestQueue(() => Interlocked.Increment(ref messagesSent));
            var sqsQueue4 = TestQueue(() => Interlocked.Increment(ref messagesSent));
            var buffer1 = CreateMessageReceiveBuffer(sqsQueue1);
            var buffer2 = CreateMessageReceiveBuffer(sqsQueue2);
            var buffer3 = CreateMessageReceiveBuffer(sqsQueue3);
            var buffer4 = CreateMessageReceiveBuffer(sqsQueue4);

            IMessageDispatcher dispatcher = TestDispatcher(() => Interlocked.Increment(ref messagesDispatched));
            IChannelConsumer consumer1 = CreateChannelConsumer(dispatcher);
            IChannelConsumer consumer2 = CreateChannelConsumer(dispatcher);
            IChannelConsumer consumer3 = CreateChannelConsumer(dispatcher);

            IMultiplexer multiplexer = CreateMultiplexer();

            multiplexer.ReadFrom(buffer1.Reader);
            multiplexer.ReadFrom(buffer2.Reader);
            multiplexer.ReadFrom(buffer3.Reader);
            multiplexer.ReadFrom(buffer4.Reader);

            consumer1.ConsumeFrom(multiplexer.Messages());
            consumer2.ConsumeFrom(multiplexer.Messages());
            consumer3.ConsumeFrom(multiplexer.Messages());

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var multiplexerCompletion = multiplexer.Run(cts.Token);

            // consumers
            var consumer1Completion = consumer1.Run(cts.Token);
            var consumer2Completion = consumer2.Run(cts.Token);
            var consumer3Completion = consumer3.Run(cts.Token);

            var buffer1Completion = buffer1.Run(cts.Token);
            var buffer2Completion = buffer2.Run(cts.Token);
            var buffer3Completion = buffer3.Run(cts.Token);
            var buffer4Completion = buffer4.Run(cts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => buffer1Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => buffer2Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => buffer3Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => buffer4Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer1Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer2Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer3Completion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => multiplexerCompletion);

            _testOutputHelper.WriteLine("Attempted to send {0} messages and dispatched {1} messages", messagesSent,
                messagesDispatched);

            messagesDispatched.ShouldBe(messagesSent);
        }

        [Fact]
        public async Task Consumer_Not_Started_No_Buffer_Filled_Then_No_More_Messages_Requested()
        {
            // Arrange
            ILogger logger = _testOutputHelper.ToLogger<ChannelConsumer>();
            int messagesFromQueue = 0;
            int messagesDispatched = 0;
            var sqsQueue = TestQueue(() => Interlocked.Increment(ref messagesFromQueue));
            IMessageReceiveBuffer buffer = CreateMessageReceiveBuffer(sqsQueue);
            IMessageDispatcher dispatcher = TestDispatcher(() => Interlocked.Increment(ref messagesDispatched));
            IChannelConsumer consumer = CreateChannelConsumer(dispatcher);
            IMultiplexer multiplexer = CreateMultiplexer();

            multiplexer.ReadFrom(buffer.Reader);
            consumer.ConsumeFrom(multiplexer.Messages());

            // need to start the multiplexer before calling Messages

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            // Act and Assert
            var multiplexerCompletion = multiplexer.Run(cts.Token);
            var bufferCompletion = buffer.Run(cts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => multiplexerCompletion);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bufferCompletion);

            messagesFromQueue.ShouldBe(111);
            messagesDispatched.ShouldBe(0);

            // Starting the consumer after the token is cancelled will not dispatch messages
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consumer.Run(cts.Token));

            messagesFromQueue.ShouldBe(111);
            messagesDispatched.ShouldBe(0);
        }

        [Fact]
        public async Task If_Queue_Is_Slow_All_Messages_Processed()
        {
            int messagesFromQueue = 0;
            int messagesDispatched = 0;
            var sqsQueue1 = TestQueue(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                messagesFromQueue++;
            });

            var queues = new List<ISqsQueue> { sqsQueue1 };
            IMessageDispatcher dispatcher = TestDispatcher(() => messagesDispatched++);
            var bus = CreateConsumerBus(queues, dispatcher);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            try
            {
                await bus.Run(cts.Token);
            }
            catch (OperationCanceledException)
            { }

            messagesFromQueue.ShouldBeGreaterThan(0);
            messagesDispatched.ShouldBe(messagesFromQueue);
        }

        [Fact]
        public async Task If_Consumer_Is_Slow_All_Messages_Processed()
        {
            int messagesFromQueue = 0;
            int messagesDispatched = 0;

            var sqsQueue1 = TestQueue(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
                messagesFromQueue++;
            });

            var queues = new List<ISqsQueue> { sqsQueue1 };
            IMessageDispatcher dispatcher = TestDispatcher(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                messagesDispatched++;
            });
            var bus = CreateConsumerBus(queues, dispatcher);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            try
            {
                await bus.Run(cts.Token);
            }
            catch (OperationCanceledException)
            { }

            messagesFromQueue.ShouldBeGreaterThan(0);
            messagesDispatched.ShouldBe(messagesFromQueue);
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

        private static IMessageDispatcher TestDispatcher(Action spy = null)
        {
            async Task OnDispatchMessage()
            {
                spy?.Invoke();
                await Task.CompletedTask;
            }

            IMessageDispatcher dispatcherMock = Substitute.For<IMessageDispatcher>();
            dispatcherMock
                .DispatchMessageAsync(Arg.Any<IQueueMessageContext>(), Arg.Any<CancellationToken>())
                .Returns(async _ => await OnDispatchMessage());

            return dispatcherMock;
        }

        private IMessageReceiveBuffer CreateMessageReceiveBuffer(ISqsQueue sqsQueue)
        {
            return new MessageReceiveBuffer(
                10,
                sqsQueue,
                new NoopSqsPolicyAsync<IList<Message>>(),
                Substitute.For<IMessageMonitor>(),
                _testOutputHelper.ToLoggerFactory());
        }

        private IChannelConsumer CreateChannelConsumer(IMessageDispatcher dispatcher)
        {
            return new ChannelConsumer(dispatcher);
        }

        private IConsumerBus CreateConsumerBus(
            IList<ISqsQueue> queues,
            IMessageDispatcher dispatcher)
        {
            var config = new ConsumerConfig();
            return new ConsumerBus(
                queues,
                config,
                dispatcher,
                Substitute.For<IMessageMonitor>(),
                _testOutputHelper.ToLoggerFactory());
        }

        private IMultiplexer CreateMultiplexer()
        {
            return new RoundRobinQueueMultiplexer(100, _testOutputHelper.ToLoggerFactory());
        }

        private class TestMessage : Message
        {
            public override string ToString()
            {
                return Body;
            }
        }
    }
}
