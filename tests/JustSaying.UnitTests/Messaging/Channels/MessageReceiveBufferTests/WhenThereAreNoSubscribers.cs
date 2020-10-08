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
    public class WhenThereAreNoSubscribers
    {
        protected class TestMessage : Message
        { }

        private int _callCount;
        private readonly MessageReceiveBuffer _messageReceiveBuffer;
        private readonly ITestOutputHelper _outputHelper;

        public WhenThereAreNoSubscribers(ITestOutputHelper testOutputHelper)
        {
            _outputHelper = testOutputHelper;
            var loggerFactory = testOutputHelper.ToLoggerFactory();

            MiddlewareBase<GetMessagesContext, IList<Message>> sqsMiddleware =
                new DelegateMiddleware<GetMessagesContext, IList<Message>>();
            var sqsClient = Substitute.For<IAmazonSQS>();
            var queue = Substitute.For<ISqsQueue>();
            queue.Uri.Returns(new Uri("http://test.com"));
            queue.Client.Returns(sqsClient);
            var monitor = new TestingFramework.TrackingLoggingMonitor(
                loggerFactory.CreateLogger<IMessageMonitor>());

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

        [Fact]
        public async Task Buffer_Is_Filled()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var _ = _messageReceiveBuffer.RunAsync(cts.Token);

            await Patiently.AssertThatAsync(_outputHelper, () => _callCount > 0);

            _callCount.ShouldBeGreaterThan(0);
        }
    }
}
