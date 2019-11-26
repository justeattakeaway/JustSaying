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
    public class CanCreateMultipleListeners
    {
        private readonly ITestOutputHelper _outputHelper;

        public CanCreateMultipleListeners(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task OneQueueNoMessages_OtherContinuesToProcessMessages()
        {
            // Arrange
            var receiptHandle = "ReceiptHandle";
            var messageBody = "Body";
            var receiver1 = CreateMessageReceiver(receiptHandle, messageBody);
            var messageDispatcher1 = Substitute.For<IMessageDispatcher>();
            messageDispatcher1.DispatchMessage(Arg.Any<Amazon.SQS.Model.Message>(), Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult(true));
            var coordinator1 = CreateMessageCoordinator(_outputHelper.ToLoggerFactory(), receiver1, messageDispatcher1);

            var receiver2 = CreateEmptyMessageReceiver();
            var messageDispatcher2 = Substitute.For<IMessageDispatcher>();
            messageDispatcher2.DispatchMessage(Arg.Any<Amazon.SQS.Model.Message>(), Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult(true));
            var coordinator2 = CreateMessageCoordinator(_outputHelper.ToLoggerFactory(), receiver1, messageDispatcher1);

            // Act
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
            _ = coordinator1.ListenAsync(cts.Token);
            _ = coordinator2.ListenAsync(cts.Token);

            // Assert
            await messageDispatcher1.Received()
                .DispatchMessage(Arg.Is<Amazon.SQS.Model.Message>(msg => msg.ReceiptHandle == receiptHandle), Arg.Any<CancellationToken>());

            await messageDispatcher2.DidNotReceive()
                .DispatchMessage(Arg.Any<Amazon.SQS.Model.Message>(), Arg.Any<CancellationToken>());
        }

        private static IMessageCoordinator CreateMessageCoordinator(
           ILoggerFactory loggerFactory,
           IMessageReceiver messageReceiver,
           IMessageDispatcher messageDispatcher = null,
           IMessageProcessingStrategy messageProcessingStrategy = null)
        {
            var logger = loggerFactory.CreateLogger("test-logger");
            messageDispatcher ??= Substitute.For<IMessageDispatcher>();

            var messageMonitor = Substitute.For<IMessageMonitor>();
            messageProcessingStrategy ??= new DefaultThrottledThroughput(messageMonitor, logger);

            return new MessageCoordinator(
                logger,
                messageReceiver,
                messageDispatcher,
                messageProcessingStrategy);
        }

        private static IMessageReceiver CreateEmptyMessageReceiver()
        {
            var messageReceiver = Substitute.For<IMessageReceiver>();
            messageReceiver.GetMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(EmptyResponse());

            return messageReceiver;
        }

        private static async Task<Amazon.SQS.Model.ReceiveMessageResponse> EmptyResponse()
        {
            await Task.Delay(500);
            return new Amazon.SQS.Model.ReceiveMessageResponse
            {
                Messages = new List<Amazon.SQS.Model.Message> { }
            };
        }

        private static IMessageReceiver CreateMessageReceiver(string receiptHandle, string messageBody)
        {
            var messageReceiver = Substitute.For<IMessageReceiver>();
            messageReceiver.GetMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new Amazon.SQS.Model.ReceiveMessageResponse
                {
                    Messages = new List<Amazon.SQS.Model.Message>
                    {
                        new Amazon.SQS.Model.Message { ReceiptHandle = receiptHandle, Body = messageBody },
                    }
                });

            return messageReceiver;
        }
    }
}
