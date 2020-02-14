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
            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer.Reader);

            // need to start the multiplexer before calling Start
            var multiplexerTask = multiplexer.Start();

            // consumer
            var t1 = Listen(multiplexer.Messages(), "one");

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
            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer.Reader);

            var multiplexerTask = multiplexer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            // consumers
            var t1 = Listen(multiplexer.Messages(), "one");
            var t2 = Listen(multiplexer.Messages(), "two");

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

            IMultiplexer multiplexer = new RoundRobinQueueMultiplexer(NullLoggerFactory.Instance);

            multiplexer.ReadFrom(buffer1.Reader);
            multiplexer.ReadFrom(buffer2.Reader);

            var multiplexerTask = multiplexer.Start();

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            // consumers
            var t1 = Listen(multiplexer.Messages(), "one");

            var writeTask1 = buffer1.Start(cts.Token);
            var writeTask2 = buffer2.Start(cts.Token);

            await writeTask1;
            await writeTask2;
            await t1;
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

        private class LoggingDispatcher // : IMessageDispatcher
        {
            private readonly ILogger _logger;
            private readonly string _prefix;
            private readonly HandlerMap _handlerMap;

            public LoggingDispatcher(
                ILogger logger,
                string prefix,
                HandlerMap handlerMap)
            {
                _logger = logger;
                _prefix = prefix;
                _handlerMap = handlerMap;
            }

            public Task DispatchMessage(Message message, CancellationToken cancellationToken)
            {
                // get message type
                // find type in HandlerMap

                _logger.LogInformation($"Dispatcher {_prefix} got message '{message}'");
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
