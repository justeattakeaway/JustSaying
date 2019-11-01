using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.NotificationListener.MessageCoordination
{
    public class CanSubscribeAndListen
    {
        private readonly ITestOutputHelper _outputHelper;

        public CanSubscribeAndListen(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task CanListen_StopsListeningWhenTokenCancelled()
        {
            var coordinator = CreateMessageCoordinator(_outputHelper.ToLoggerFactory());
            var cts = new CancellationTokenSource(1000);
            var listening = false;
            _ = Task.Run(async () =>
            {
                await coordinator.ListenAsync(cts.Token).ConfigureAwait(false);
                listening = false;
            });
            listening = true;

            Assert.True(listening);

            await Task.Delay(2000);

            Assert.False(listening);
        }

        [Fact]
        public async Task MessageResponseIsValid_MessageIsHandled()
        {
            // Arrange
            var receiptHandle = "ReceiptHandle";
            var messageBody = "Body";
            var messageReceiver = Substitute.For<IMessageReceiver>();
            messageReceiver.GetMessages(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new Amazon.SQS.Model.ReceiveMessageResponse
                {
                    Messages = new List<Amazon.SQS.Model.Message>
                    {
                        new Amazon.SQS.Model.Message { ReceiptHandle = receiptHandle, Body = messageBody },
                    }
                });
            var messageDispatcher = Substitute.For<IMessageDispatcher>();

            var coordinator = CreateMessageCoordinator(_outputHelper.ToLoggerFactory(), messageReceiver, messageDispatcher);

            // Act
            var cts = new CancellationTokenSource(1000);
            _ = coordinator.ListenAsync(cts.Token);

            await Task.Delay(500);

            // Assert
            await messageDispatcher.Received()
                .DispatchMessage(
                    Arg.Is<Amazon.SQS.Model.Message>(
                        msg => msg.ReceiptHandle == receiptHandle && msg.Body == messageBody),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SqsRequestThrowsException_ContinuesToRequestMessages()
        {
            // Arrange
            var messageDispatcher = Substitute.For<IMessageDispatcher>();
            var messageReceiver = Substitute.For<IMessageReceiver>();
            messageReceiver.GetMessages(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns<Task<Amazon.SQS.Model.ReceiveMessageResponse>>(_ => throw new Exception("Cannot retrieve messages!"));
            var coordinator = CreateMessageCoordinator(_outputHelper.ToLoggerFactory(), messageReceiver, messageDispatcher);

            // Act
            var cts = new CancellationTokenSource();
            cts.CancelAfter(1000);
            _ = Task.Run(() => coordinator.ListenAsync(cts.Token));

            await Task.Delay(500);

            // Assert
            await messageReceiver.Received()
                 .GetMessages(Arg.Any<int>(), Arg.Any<CancellationToken>());
            messageReceiver.ClearReceivedCalls();
            await Task.Delay(500);
            await messageReceiver.Received()
                 .GetMessages(Arg.Any<int>(), Arg.Any<CancellationToken>());

            await messageDispatcher.DidNotReceive()
                 .DispatchMessage(Arg.Any<Amazon.SQS.Model.Message>(), Arg.Any<CancellationToken>());
        }

        private static IMessageCoordinator CreateMessageCoordinator(
            ILoggerFactory loggerFactory,
            IMessageReceiver messageReceiver = null,
            IMessageDispatcher messageDispatcher = null)
        {
            var logger = loggerFactory.CreateLogger("test-logger");
            messageReceiver ??= Substitute.For<IMessageReceiver>();
            messageDispatcher ??= Substitute.For<IMessageDispatcher>();

            var messageMonitor = Substitute.For<IMessageMonitor>();
            var messageProcessingStrategy = new DefaultThrottledThroughput(messageMonitor, logger);

            return new MessageCoordinator(
                logger,
                messageReceiver,
                messageDispatcher,
                messageProcessingStrategy);
        }
    }
}
