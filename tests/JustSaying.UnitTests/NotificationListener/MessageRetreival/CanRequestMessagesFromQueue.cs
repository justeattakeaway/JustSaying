using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.NotificationListener.MessageRetreival
{
    public class CanRequestMessagesFromQueue
    {
        private readonly ITestOutputHelper _outputHelper;

        public CanRequestMessagesFromQueue(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task RequestToSqsSucceeds_ResponseIsReturned()
        {
            var response = CreateResponse();
            var sqsClient = CreateSqsClient(response);
            var receiver = CreateMessageReceiver(_outputHelper.ToLoggerFactory(), sqsClient);

            var messages = await receiver.GetMessagesAsync(1, CancellationToken.None);

            Assert.Equal(response, messages);
        }

        [Fact]
        public async Task RequestToSqsFails_ResponseIsNull()
        {
            var sqsClient = CreateSqsClientWithException();
            var receiver = CreateMessageReceiver(_outputHelper.ToLoggerFactory(), sqsClient);

            var messages = await receiver.GetMessagesAsync(1, CancellationToken.None);

            Assert.Null(messages);
        }

        private static IMessageReceiver CreateMessageReceiver(
            ILoggerFactory loggerFactory, IAmazonSQS sqsClient)
        {
            loggerFactory ??= Substitute.For<ILoggerFactory>();

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

        private static IAmazonSQS CreateSqsClient(Amazon.SQS.Model.ReceiveMessageResponse response)
        {
            var sqsClient = Substitute.For<IAmazonSQS>();
            sqsClient.ReceiveMessageAsync(Arg.Any<Amazon.SQS.Model.ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(response);

            return sqsClient;
        }

        private static IAmazonSQS CreateSqsClientWithException()
        {
            var sqsClient = Substitute.For<IAmazonSQS>();
            sqsClient.ReceiveMessageAsync(Arg.Any<Amazon.SQS.Model.ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs<Amazon.SQS.Model.ReceiveMessageResponse>(x => throw new Exception("Test exception"));

            return sqsClient;
        }

        private static Amazon.SQS.Model.ReceiveMessageResponse CreateResponse()
        {
            return new Amazon.SQS.Model.ReceiveMessageResponse
            {
                Messages = new List<Amazon.SQS.Model.Message>
                {
                    new Amazon.SQS.Model.Message { ReceiptHandle = "hello", Body = "Not testing this" },
                }
            };
        }
    }
}
