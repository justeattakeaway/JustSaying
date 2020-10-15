using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.MessageReceiveBufferTests
{
    public class WhenSubscriberIsSlow
    {
        protected class TestMessage : Message { }

        private int _callCount;
        private readonly MessageReceiveBuffer _messageReceiveBuffer;

        public WhenSubscriberIsSlow(ITestOutputHelper testOutputHelper)
        {
            var loggerFactory = testOutputHelper.ToLoggerFactory();

            MiddlewareBase<GetMessagesContext, IList<Message>> sqsMiddleware =
                new DelegateMiddleware<GetMessagesContext, IList<Message>>();
            var sqsClient = Substitute.For<IAmazonSQS>();
            var queue = Substitute.For<ISqsQueue>();
            queue.Uri.Returns(new Uri("http://test.com"));
            queue.Client.Returns(sqsClient);
            var monitor = new TestingFramework.TrackingLoggingMonitor(
                loggerFactory.CreateLogger<TrackingLoggingMonitor>());

            sqsClient.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    Interlocked.Increment(ref _callCount);
                    var messages = new List<Message> { new TestMessage() };
                    return new ReceiveMessageResponse { Messages = messages };
                });

            _messageReceiveBuffer = new MessageReceiveBuffer(
                10,
                10,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                queue,
                sqsMiddleware,
                monitor,
                loggerFactory.CreateLogger<IMessageReceiveBuffer>());
        }

        protected async Task<int> Messages()
        {
            int messagesProcessed = 0;

            while (true)
            {
                await Task.Delay(100);
                var couldRead = await _messageReceiveBuffer.Reader.WaitToReadAsync();
                if (!couldRead) break;

                while (_messageReceiveBuffer.Reader.TryRead(out var _))
                {
                    messagesProcessed++;
                }
            }

            return messagesProcessed;
        }

        [Fact]
        public async Task All_Messages_Are_Processed()
        {
            using var cts = new CancellationTokenSource();
            var _ = _messageReceiveBuffer.RunAsync(cts.Token);
            var readTask = Messages();

            // Read messages for a while
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Cancel token
            cts.Cancel();

            // Ensure buffer completes
            await _messageReceiveBuffer.Reader.Completion;

            // Get the number of messages we read
            var messagesRead = await readTask;

            // Make sure that number makes sense
            messagesRead.ShouldBeGreaterThan(0);
            messagesRead.ShouldBeLessThanOrEqualTo(_callCount);
        }
    }
}
