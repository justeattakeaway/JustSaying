using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class WhenUsingSqsQueueByName : BaseSubscriptionGroupTests
    {
        private ISqsQueue _queue;
        private int _callCount;
        private IAmazonSQS _client;
        protected readonly string MessageTypeString = typeof(SimpleMessage).ToString();
        protected const string MessageBody = "object";

        public WhenUsingSqsQueueByName(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            int retryCount = 1;

            _client = Substitute.For<IAmazonSQS>();
            var response = GenerateResponseMessage(MessageTypeString, Guid.NewGuid());

            _client.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(
                    x => response,
                    async x =>
                    {
                        await Task.Delay(30);
                        return new ReceiveMessageResponse();
                    });

            _client.GetQueueUrlAsync(Arg.Any<string>())
                .Returns(x =>
                {
                    if (x.Arg<string>() == "some-queue-name")
                        return new GetQueueUrlResponse
                        {
                            QueueUrl = "https://testqueues.com/some-queue-name"
                        };
                    throw new QueueDoesNotExistException("some-queue-name not found");
                });

            _client.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
                .Returns(new GetQueueAttributesResponse()
                {
                    Attributes = new Dictionary<string, string> { { "QueueArn", "something:some-queue-name" } }
                });

            var queue = new SqsQueueByName(RegionEndpoint.EUWest1, "some-queue-name", _client, retryCount, LoggerFactory);
            queue.ExistsAsync().Wait();

            _queue = queue;

            Queues.Add(_queue);
            Handler.Handle(null)
                .ReturnsForAnyArgs(true).AndDoes(ci => Interlocked.Increment(ref _callCount));
        }

        [Fact]
        public void HandlerReceivesMessage()
        {
            Handler.Received().Handle(SerializationRegister.DefaultDeserializedMessage());
        }

        protected static async Task<ReceiveMessageResponse> GenerateResponseMessage(string messageType, Guid messageId)
        {
            await Task.Delay(30);
            return new ReceiveMessageResponse
            {
                Messages = new List<Message>
                {
                    new Message
                    {
                        MessageId = messageId.ToString(),
                        Body = SqsMessageBody(messageType)
                    },
                    new Message
                    {
                        MessageId = messageId.ToString(),
                        Body = "{\"Subject\":\"SOME_UNKNOWN_MESSAGE\"," + "\"Message\":\"SOME_RANDOM_MESSAGE\"}"
                    }
                }
            };
        }

        protected static string SqsMessageBody(string messageType)
        {
            return "{\"Subject\":\"" + messageType + "\"," + "\"Message\":\"" + MessageBody + "\"}";
        }
    }
}
