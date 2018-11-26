using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs
{
    public class WhenPublishing : XAsyncBehaviourTest<SqsPublisher>
    {
        private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private const string Url = "https://blablabla/" + QueueName;
        private readonly SimpleMessage _message = new SimpleMessage { Content = "Hello" };
        private const string QueueName = "queuename";

        protected override async Task<SqsPublisher> CreateSystemUnderTestAsync()
        {
            var sqs = new SqsPublisher(RegionEndpoint.EUWest1, QueueName, _sqs, 0, _serializationRegister, Substitute.For<ILoggerFactory>());
            await sqs.ExistsAsync();
            return sqs;
        }

        protected override Task Given()
        {
            _sqs.GetQueueUrlAsync(Arg.Any<string>()).Returns(new GetQueueUrlResponse {QueueUrl = Url});
            _sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>()).Returns(new GetQueueAttributesResponse());
            _serializationRegister.Serialize(_message, false).Returns("serialized_contents");
            return Task.CompletedTask;
        }

        protected override async Task When()
        {
            await SystemUnderTest.PublishAsync(_message, CancellationToken.None);
        }

        [Fact]
        public void MessageIsPublishedToQueue()
        {
            // ToDo: Could be better...
            _sqs.Received().SendMessageAsync(Arg.Is<SendMessageRequest>(x => x.MessageBody.Equals("serialized_contents")));
        }

        [Fact]
        public void MessageIsPublishedToCorrectLocation()
        {
            _sqs.Received().SendMessageAsync(Arg.Is<SendMessageRequest>(x => x.QueueUrl == Url));
        }
    }
}
