using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Amazon;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.UnitTests.NotificationListener.MessageRetreival
{
    public class CanRequestMessagesFromQueue
    {
        private static IMessageReceiver CreateMessageReceiver(ILoggerFactory loggerFactory, IAmazonSQS sqsClient = null)
        {
            loggerFactory ??= Substitute.For<ILoggerFactory>();

            sqsClient ??= CreateSqsClient();

            var queue = Substitute.For<ISqsQueue>();
            queue.Region.Returns(RegionEndpoint.EUWest2);
            queue.QueueName.Returns("test-queue");
            queue.Uri.Returns(new Uri("http://localhost"));
            queue.Client.Returns(sqsClient);

            var messagingMonitor = Substitute.For<IMessageMonitor>();
            var logger = loggerFactory.CreateLogger<MessageReceiver>();
            var messageBackoffStrategy = Substitute.For<IMessageBackoffStrategy>();

            return new MessageReceiver(queue, messagingMonitor, logger, messageBackoffStrategy);
        }

        private static IAmazonSQS CreateSqsClient()
        {
            var sqsClient = Substitute.For<IAmazonSQS>();
            sqsClient.ReceiveMessageAsync(Arg.Any<Amazon.SQS.Model.ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(new Amazon.SQS.Model.ReceiveMessageResponse
                {
                    Messages = new List<Amazon.SQS.Model.Message>
                    {
                        new Amazon.SQS.Model.Message { ReceiptHandle = "hello", Body = "Not testing this" },
                    }
                });

            return sqsClient;
        }

        private static IAmazonSQS CreateSqsClientWithException()
        {
            var sqsClient = Substitute.For<IAmazonSQS>();
            sqsClient.ReceiveMessageAsync(Arg.Any<Amazon.SQS.Model.ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs<Amazon.SQS.Model.ReceiveMessageResponse>(x => throw new Exception("Test exception"));

            return sqsClient;
        }
    }
}
