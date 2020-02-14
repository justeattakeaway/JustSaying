using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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
            IMessageDispatcher dispatcher = new LoggingDispatcher(_testOutputHelper.ToLogger<LoggingDispatcher>(), "one");
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
            IMessageDispatcher dispatcher1 = new LoggingDispatcher(_testOutputHelper.ToLogger<LoggingDispatcher>(), "one");
            IMessageDispatcher dispatcher2 = new LoggingDispatcher(_testOutputHelper.ToLogger<LoggingDispatcher>(), "two");
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

            IMessageDispatcher dispatcher1 = new LoggingDispatcher(_testOutputHelper.ToLogger<LoggingDispatcher>(), "one");
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
            IMessageDispatcher dispatcher1 = new LoggingDispatcher(_testOutputHelper.ToLogger<LoggingDispatcher>(), "one");
            IMessageDispatcher dispatcher2 = new LoggingDispatcher(_testOutputHelper.ToLogger<LoggingDispatcher>(), "two");
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

        private static ISqsQueue TestQueue(string prefix)
        {
            var sqsQueueMock = new Mock<ISqsQueue>();
            sqsQueueMock.Setup(q => q.GetMessages(It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(5).ConfigureAwait(false);
                    return new List<Message>
                    {
                        new TestMessage { Body = prefix },
                    };
                });

            return sqsQueueMock.Object;
        }

        private async Task Listen(IAsyncEnumerable<IQueueMessageContext> messages, string prefix)
        {
            await foreach (var msg in messages)
            {
                var mes = msg.Message?.ToString();
                _testOutputHelper.WriteLine($"{prefix}-{mes}");
                await Task.Delay(5).ConfigureAwait(false);
            }
        }

        private class LoggingDispatcher : IMessageDispatcher
        {
            private readonly ILogger _logger;
            private readonly string _prefix;

            public LoggingDispatcher(
                ILogger logger,
                string prefix)
            {
                _logger = logger;
                _prefix = prefix;
            }

            public Task DispatchMessage(IQueueMessageContext messageContext, CancellationToken cancellationToken)
            {
                // get message type
                // find type in HandlerMap

                _logger.LogInformation($"Dispatcher {_prefix} got message '{messageContext.Message}'");
                return Task.CompletedTask;
            }
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
