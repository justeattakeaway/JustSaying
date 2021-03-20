using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
#pragma warning disable 618

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs
{
    public class WhenPublishingAsync : WhenPublishingTestBase
    {
        private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
        private const string Url = "https://blablabla/" + QueueName;
        private readonly SimpleMessage _message = new SimpleMessage { Content = "Hello" };
        private const string QueueName = "queuename";

        private protected override async Task<SqsPublisher> CreateSystemUnderTestAsync()
        {
            var sqs = new SqsPublisher(RegionEndpoint.EUWest1, QueueName, Sqs, 0, _serializationRegister, Substitute.For<ILoggerFactory>());
            await sqs.ExistsAsync();
            return sqs;
        }

        protected override void Given()
        {
            Sqs.GetQueueUrlAsync(Arg.Any<string>())
                 .Returns(new GetQueueUrlResponse { QueueUrl = Url });

            Sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
                 .Returns(new GetQueueAttributesResponse());

            _serializationRegister.Serialize(_message, false)
                .Returns("serialized_contents");
        }

        protected override async Task WhenAsync()
        {
            await SystemUnderTest.PublishAsync(_message);
        }

        [Fact]
        public void MessageIsPublishedToQueue()
        {
            // ToDo: Could be better...
            Sqs.Received().SendMessageAsync(Arg.Is<SendMessageRequest>(
                x => x.MessageBody.Equals("serialized_contents", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void MessageIsPublishedToCorrectLocation()
        {
            Sqs.Received().SendMessageAsync(Arg.Is<SendMessageRequest>(x => x.QueueUrl == Url));
        }
    }
}
