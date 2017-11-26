using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.Sqs
{
    public class WhenPublishing : XAsyncBehaviourTest<SqsPublisher>
    {
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private const string Url = "https://blablabla/" + QueueName;
        private readonly GenericMessage _message = new GenericMessage {Content = "Hello"};
        private const string QueueName = "queuename";

        protected override SqsPublisher CreateSystemUnderTest()
        {
            var sqs = new SqsPublisher(RegionEndpoint.EUWest1, QueueName, _sqs, 0, _serialisationRegister, Substitute.For<ILoggerFactory>());
            sqs.Exists();
            return sqs;
        }

        protected override void Given()
        {
            _sqs.GetQueueUrlAsync(Arg.Any<string>()).Returns(new GetQueueUrlResponse {QueueUrl = Url});
            _sqs.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>()).Returns(new GetQueueAttributesResponse());
            _serialisationRegister.Serialise(_message, false).Returns("serialized_contents");
        }

        protected override async Task When()
        {
            await SystemUnderTest.PublishAsync(_message);
        }

        [Fact]
        public void MessageIsPublishedToQueue()
        {
            // ToDo: Can be better...
            _sqs.Received().SendMessageAsync(Arg.Is<SendMessageRequest>(x => x.MessageBody.Equals("serialized_contents")));
        }

        [Fact]
        public void MessageIsPublishedToCorrectLocation()
        {
            _sqs.Received().SendMessageAsync(Arg.Is<SendMessageRequest>(x => x.QueueUrl == Url));
        }
    }
}
