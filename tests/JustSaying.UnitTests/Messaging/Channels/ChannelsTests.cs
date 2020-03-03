using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

        [Fact]
        public async Task QueueCanBeAssignedToOnePump()
        {
            var sqsQueue = TestQueue("one");
            var buffer = new MessageReceiveBuffer(10, sqsQueue, Substitute.For<IMessageMonitor>(),
                NullLoggerFactory.Instance);
            IMessageDispatcher dispatcher = TestDispatcher();
            IChannelConsumer consumer = new ChannelConsumer(dispatcher, NullLoggerFactory.Instance);
            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer.Reader);
            consumer.ConsumeFrom(multiplexer.Messages());

            // need to start the multiplexer before calling Start
            await multiplexer.Start();

            // consumer
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var consumer1Completion = consumer.Start(cts.Token);
            var buffer1Completion = buffer.Start(cts.Token);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                Task.WhenAll(multiplexer.Completion, consumer1Completion, buffer1Completion));
        }

        [Fact]
        public async Task QueueCanBeAssignedToMultiplePumps()
        {
            var sqsQueue = TestQueue("one");
            var buffer = new MessageReceiveBuffer(10, sqsQueue, Substitute.For<IMessageMonitor>(),
                NullLoggerFactory.Instance);

            // using 2 dispatchers for logging, they should be the same/stateless
            IMessageDispatcher dispatcher1 = TestDispatcher();
            IMessageDispatcher dispatcher2 = TestDispatcher();
            IChannelConsumer consumer1 = new ChannelConsumer(dispatcher1, NullLoggerFactory.Instance);
            IChannelConsumer consumer2 = new ChannelConsumer(dispatcher2, NullLoggerFactory.Instance);

            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer.Reader);
            consumer1.ConsumeFrom(multiplexer.Messages());
            consumer2.ConsumeFrom(multiplexer.Messages());

            await multiplexer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            // consumers
            var t1 = consumer1.Start(cts.Token);
            var t2 = consumer2.Start(cts.Token);

            var completion = buffer.Start(cts.Token);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                Task.WhenAll(t1, t2, completion, multiplexer.Completion));
        }

        [Fact]
        public async Task MultipleQueuesCanBeAssignedToOnePump()
        {
            var sqsQueue1 = TestQueue("one");
            var sqsQueue2 = TestQueue("two");
            var buffer1 = new MessageReceiveBuffer(10, sqsQueue1, Substitute.For<IMessageMonitor>(),
                NullLoggerFactory.Instance);
            var buffer2 = new MessageReceiveBuffer(10, sqsQueue2, Substitute.For<IMessageMonitor>(),
                NullLoggerFactory.Instance);

            IMessageDispatcher dispatcher1 = TestDispatcher();
            IChannelConsumer consumer = new ChannelConsumer(dispatcher1, NullLoggerFactory.Instance);

            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer1.Reader);
            multiplexer.ReadFrom(buffer2.Reader);

            consumer.ConsumeFrom(multiplexer.Messages());

            await multiplexer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            // consumers
            var consumer1Completion = consumer.Start(cts.Token);

            var buffer1Completion = buffer1.Start(cts.Token);
            var buffer2Completion = buffer2.Start(cts.Token);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                Task.WhenAll(buffer1Completion, buffer2Completion, consumer1Completion, multiplexer.Completion));
        }

        [Fact]
        public async Task MultipleQueuesCanBeAssignedToMultiplePumps()
        {
            var sqsQueue1 = TestQueue("one");
            var sqsQueue2 = TestQueue("two");
            var buffer1 = new MessageReceiveBuffer(10, sqsQueue1, Substitute.For<IMessageMonitor>(),
                NullLoggerFactory.Instance);
            var buffer2 = new MessageReceiveBuffer(10, sqsQueue2, Substitute.For<IMessageMonitor>(),
                NullLoggerFactory.Instance);

            // using 2 dispatchers for logging, they should be the same/stateless
            IMessageDispatcher dispatcher1 = TestDispatcher();
            IMessageDispatcher dispatcher2 = TestDispatcher();
            IChannelConsumer consumer1 = new ChannelConsumer(dispatcher1, NullLoggerFactory.Instance);
            IChannelConsumer consumer2 = new ChannelConsumer(dispatcher2, NullLoggerFactory.Instance);

            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer1.Reader);
            multiplexer.ReadFrom(buffer2.Reader);

            consumer1.ConsumeFrom(multiplexer.Messages());
            consumer2.ConsumeFrom(multiplexer.Messages());

            await multiplexer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            // consumers
            var consumer1Completion = consumer1.Start(cts.Token);
            var consumer2Completion = consumer2.Start(cts.Token);

            var buffer1Completion = buffer1.Start(cts.Token);
            var buffer2Completion = buffer2.Start(cts.Token);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                Task.WhenAll(buffer1Completion, buffer2Completion, consumer1Completion, consumer2Completion,
                    multiplexer.Completion));
        }

        [Fact]
        public async Task All_Messages_Are_Processed()
        {
            int messagesFromQueue = 0;
            int messagesDispatched = 0;
            var sqsQueue = TestQueue("one", () => messagesFromQueue++);

            IMessageReceiveBuffer buffer = new MessageReceiveBuffer(10, sqsQueue, Substitute.For<IMessageMonitor>(),
                NullLoggerFactory.Instance);
            IMessageDispatcher dispatcher = TestDispatcher(() => messagesDispatched++);
            IChannelConsumer consumer = new ChannelConsumer(dispatcher, NullLoggerFactory.Instance);
            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer.Reader);
            consumer.ConsumeFrom(multiplexer.Messages());

            // need to start the multiplexer before calling Messages
            await multiplexer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            // consumer
            var consumer1Completion = consumer.Start(cts.Token);

            var bufferCompletion = buffer.Start(cts.Token);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                Task.WhenAll(bufferCompletion, consumer1Completion, multiplexer.Completion));

            messagesDispatched.ShouldBe(messagesFromQueue);
        }

        [Fact]
        public async Task Can_Be_Set_Up_Using_ConsumerBus()
        {
            var sqsQueue1 = TestQueue("one");
            var sqsQueue2 = TestQueue("two");
            var sqsQueue3 = TestQueue("three");

            var queues = new List<ISqsQueue> {sqsQueue1, sqsQueue2, sqsQueue3};
            IMessageDispatcher dispatcher = TestDispatcher();
            var bus = new ConsumerBus(queues, 1, dispatcher, Substitute.For<IMessageMonitor>(),
                _testOutputHelper.ToLoggerFactory());

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            await bus.Start(cts.Token);
        }

        [Fact]
        public async Task On_Cancellation_All_Downloaded_Messages_Should_Be_Processed()
        {
            int messagesSent = 0;
            int messagesDispatched = 0;

            var sqsQueue1 = TestQueue("one", () => Interlocked.Increment(ref messagesSent));
            var sqsQueue2 = TestQueue("two", () => Interlocked.Increment(ref messagesSent));
            var sqsQueue3 = TestQueue("three", () => Interlocked.Increment(ref messagesSent));
            var sqsQueue4 = TestQueue("four", () => Interlocked.Increment(ref messagesSent));
            var buffer1 = new MessageReceiveBuffer(10, sqsQueue1, Substitute.For<IMessageMonitor>(),
                NullLoggerFactory.Instance);
            var buffer2 = new MessageReceiveBuffer(10, sqsQueue2, Substitute.For<IMessageMonitor>(),
                NullLoggerFactory.Instance);
            var buffer3 = new MessageReceiveBuffer(10, sqsQueue3, Substitute.For<IMessageMonitor>(),
                NullLoggerFactory.Instance);
            var buffer4 = new MessageReceiveBuffer(10, sqsQueue4, Substitute.For<IMessageMonitor>(),
                NullLoggerFactory.Instance);

            IMessageDispatcher dispatcher1 = TestDispatcher(() => Interlocked.Increment(ref messagesDispatched));
            IChannelConsumer consumer1 = new ChannelConsumer(dispatcher1, NullLoggerFactory.Instance);
            IChannelConsumer consumer2 = new ChannelConsumer(dispatcher1, NullLoggerFactory.Instance);
            IChannelConsumer consumer3 = new ChannelConsumer(dispatcher1, NullLoggerFactory.Instance);

            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer1.Reader);
            multiplexer.ReadFrom(buffer2.Reader);
            multiplexer.ReadFrom(buffer3.Reader);
            multiplexer.ReadFrom(buffer4.Reader);

            consumer1.ConsumeFrom(multiplexer.Messages());
            consumer2.ConsumeFrom(multiplexer.Messages());
            consumer3.ConsumeFrom(multiplexer.Messages());

            var multiplexerTask = multiplexer.Start();

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // consumers
            var t1 = consumer1.Start(cts.Token);
            var t2 = consumer2.Start(cts.Token);
            var t3 = consumer3.Start(cts.Token);

            var writeTask1 = buffer1.Start(cts.Token);
            var writeTask2 = buffer2.Start(cts.Token);
            var writeTask3 = buffer3.Start(cts.Token);
            var writeTask4 = buffer4.Start(cts.Token);

            await writeTask1;
            await writeTask2;
            await writeTask3;
            await writeTask4;
            await t1;
            await t2;
            await t3;
            await multiplexerTask;

            _testOutputHelper.WriteLine("Attempted to send {0} messages and dispatched {1} messages", messagesSent,
                messagesDispatched);

            messagesDispatched.ShouldBe(messagesSent);
        }

        [Fact]
        public async Task Consumer_Not_Started_No_Buffer_Filled_Then_No_More_Messages_Requested()
        {
            int messagesFromQueue = 0;
            int messagesDispatched = 0;
            var sqsQueue = TestQueue("one", () => messagesFromQueue++);
            var buffer = new MessageReceiveBuffer(10, sqsQueue, Substitute.For<IMessageMonitor>(),
                NullLoggerFactory.Instance);
            IMessageDispatcher dispatcher = TestDispatcher(() => messagesDispatched++);
            IChannelConsumer consumer = new ChannelConsumer(dispatcher, NullLoggerFactory.Instance);
            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer.Reader);
            consumer.ConsumeFrom(multiplexer.Messages());

            // need to start the multiplexer before calling Messages
            await multiplexer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var bufferCompletion = buffer.Start(cts.Token);

            messagesFromQueue.ShouldBe(111);
            messagesDispatched.ShouldBe(0);

            var consumer1Completion = consumer.Start(cts.Token);

            Assert.ThrowsAsync<OperationCanceledException>(() =>
                Task.WhenAll(bufferCompletion, consumer1Completion, multiplexer.Completion));

            messagesFromQueue.ShouldBe(111);
            messagesDispatched.ShouldBe(111);
        }

        private static ISqsQueue TestQueue(string prefix, Action spy = null)
        {
            async Task<IList<Message>> GetMessages()
            {
                await Task.Delay(5).ConfigureAwait(false);
                spy?.Invoke();
                return new List<Message>
                {
                    new TestMessage {Body = prefix},
                };
            }

            ISqsQueue sqsQueueMock = Substitute.For<ISqsQueue>();
            sqsQueueMock
                .GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(async _ => await GetMessages());

            return sqsQueueMock;
        }

        private static IMessageDispatcher TestDispatcher(Action spy = null)
        {
            async Task OnDispatchMessage()
            {
                await Task.Delay(5).ConfigureAwait(false);
                spy?.Invoke();
            }

            IMessageDispatcher dispatcherMock = Substitute.For<IMessageDispatcher>();
            dispatcherMock
                .DispatchMessageAsync(Arg.Any<IQueueMessageContext>(), Arg.Any<CancellationToken>())
                .Returns(async _ => await OnDispatchMessage());

            return dispatcherMock;
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
