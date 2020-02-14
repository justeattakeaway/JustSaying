using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

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
            var buffer = new DownloadBuffer(10, sqsQueue);
            IMessageDispatcher dispatcher = TestDispatcher();
            IChannelConsumer consumer = new ChannelConsumer(dispatcher);
            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer.Reader);
            consumer.ConsumeFrom(multiplexer.Messages());

            // need to start the multiplexer before calling Start
            var multiplexerTask = multiplexer.Start();

            // consumer
            var t1 = consumer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            await buffer.Start(cts.Token);

            await Task.WhenAll(multiplexerTask, t1);
        }

        [Fact]
        public async Task QueueCanBeAssignedToMultiplePumps()
        {
            var sqsQueue = TestQueue("one");
            var buffer = new DownloadBuffer(10, sqsQueue);

            // using 2 dispatchers for logging, they should be the same/stateless
            IMessageDispatcher dispatcher1 = TestDispatcher();
            IMessageDispatcher dispatcher2 = TestDispatcher();
            IChannelConsumer consumer1 = new ChannelConsumer(dispatcher1);
            IChannelConsumer consumer2 = new ChannelConsumer(dispatcher2);

            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer.Reader);
            consumer1.ConsumeFrom(multiplexer.Messages());
            consumer2.ConsumeFrom(multiplexer.Messages());

            var multiplexerTask = multiplexer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            // consumers
            var t1 = consumer1.Start();
            var t2 = consumer2.Start();

            await buffer.Start(cts.Token);

            await Task.WhenAll(t1, t2, multiplexerTask);
        }

        [Fact]
        public async Task MultipleQueuesCanBeAssignedToOnePump()
        {
            var sqsQueue1 = TestQueue("one");
            var sqsQueue2 = TestQueue("two");
            var buffer1 = new DownloadBuffer(10, sqsQueue1);
            var buffer2 = new DownloadBuffer(10, sqsQueue2);

            IMessageDispatcher dispatcher1 = TestDispatcher();
            IChannelConsumer consumer = new ChannelConsumer(dispatcher1);

            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer1.Reader);
            multiplexer.ReadFrom(buffer2.Reader);

            consumer.ConsumeFrom(multiplexer.Messages());

            var multiplexerTask = multiplexer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            // consumers
            var t1 = consumer.Start();

            var writeTask1 = buffer1.Start(cts.Token);
            var writeTask2 = buffer2.Start(cts.Token);

            await writeTask1;
            await writeTask2;
            await t1;
            await multiplexerTask;
        }

        [Fact]
        public async Task MultipleQueuesCanBeAssignedToMultiplePumps()
        {
            var sqsQueue1 = TestQueue("one");
            var sqsQueue2 = TestQueue("two");
            var buffer1 = new DownloadBuffer(10, sqsQueue1);
            var buffer2 = new DownloadBuffer(10, sqsQueue2);

            // using 2 dispatchers for logging, they should be the same/stateless
            IMessageDispatcher dispatcher1 = TestDispatcher();
            IMessageDispatcher dispatcher2 = TestDispatcher();
            IChannelConsumer consumer1 = new ChannelConsumer(dispatcher1);
            IChannelConsumer consumer2 = new ChannelConsumer(dispatcher2);

            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer1.Reader);
            multiplexer.ReadFrom(buffer2.Reader);

            consumer1.ConsumeFrom(multiplexer.Messages());
            consumer2.ConsumeFrom(multiplexer.Messages());

            var multiplexerTask = multiplexer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            // consumers
            var t1 = consumer1.Start();
            var t2 = consumer2.Start();

            var writeTask1 = buffer1.Start(cts.Token);
            var writeTask2 = buffer2.Start(cts.Token);

            await writeTask1;
            await writeTask2;
            await t1;
            await t2;
            await multiplexerTask;
        }

        [Fact]
        public async Task All_Messages_Are_Processed()
        {
            int messagesFromQueue = 0;
            int messagesDispatched = 0;
            var sqsQueue = TestQueue("one", () => messagesFromQueue++);
            var buffer = new DownloadBuffer(10, sqsQueue);
            IMessageDispatcher dispatcher = TestDispatcher(() => messagesDispatched++);
            IChannelConsumer consumer = new ChannelConsumer(dispatcher);
            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer.Reader);
            consumer.ConsumeFrom(multiplexer.Messages());

            // need to start the multiplexer before calling Messages
            var multiplexerTask = multiplexer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            // consumer
            var t1 = consumer.Start();

            await buffer.Start(cts.Token);

            await Task.WhenAll(multiplexerTask, t1);

            messagesDispatched.ShouldBe(messagesFromQueue);
        }

        [Fact]
        public async Task Consumer_Not_Started_No_Buffer_Filled_Then_No_More_Messages_Requested()
        {
            int messagesFromQueue = 0;
            int messagesDispatched = 0;
            var sqsQueue = TestQueue("one", () => messagesFromQueue++);
            var buffer = new DownloadBuffer(10, sqsQueue);
            IMessageDispatcher dispatcher = TestDispatcher(() => messagesDispatched++);
            IChannelConsumer consumer = new ChannelConsumer(dispatcher);
            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer.Reader);
            consumer.ConsumeFrom(multiplexer.Messages());

            // need to start the multiplexer before calling Messages
            var multiplexerTask = multiplexer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            await buffer.Start(cts.Token);

            messagesFromQueue.ShouldBe(21);
            messagesDispatched.ShouldBe(0);

            var t1 = consumer.Start();

            await Task.WhenAll(multiplexerTask, t1);

            messagesFromQueue.ShouldBe(21);
            messagesDispatched.ShouldBe(21);
        }

        private static ISqsQueue TestQueue(string prefix, Action spy = null)
        {
            async Task<IList<Message>> GetMessages()
            {
                await Task.Delay(5).ConfigureAwait(false);
                spy?.Invoke();
                return new List<Message>
                {
                    new TestMessage { Body = prefix },
                };
            }

            ISqsQueue sqsQueueMock = Substitute.For<ISqsQueue>();
            sqsQueueMock
                .GetMessages(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
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
                .DispatchMessage(Arg.Any<IQueueMessageContext>(), Arg.Any<CancellationToken>())
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
