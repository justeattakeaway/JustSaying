using System.Collections.Generic;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.Sqs
{
    public class WhenPublishing : BehaviourTest<SqsPublisher>
    {
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private const string Url = "https://blablabla/" + QueueName;
        private readonly GenericMessage _message = new GenericMessage {Content = "Hello"};
        private const string QueueName = "queuename";

        protected override SqsPublisher CreateSystemUnderTest()
        {
            var sqs = new SqsPublisher(RegionEndpoint.EUWest1, QueueName, _sqs, 0, _serialisationRegister);
            sqs.Exists();
            return sqs;
        }

        protected override void Given()
        {
            _sqs.GetQueueUrl(Arg.Any<string>()).Returns(new GetQueueUrlResponse {QueueUrl = Url});
            _sqs.GetQueueAttributes(Arg.Any<GetQueueAttributesRequest>()).Returns(new GetQueueAttributesResponse());
            _serialisationRegister.Serialise(_message, false).Returns("serialized_contents");
        }

        protected override void When()
        {
            SystemUnderTest.Publish(_message);
        }

        [Then]
        public void MessageIsPublishedToQueue()
        {
            // ToDo: Can be better...
            _sqs.Received().SendMessage(Arg.Is<SendMessageRequest>(x => x.MessageBody.Equals("serialized_contents")));
        }

        [Then]
        public void MessageIsPublishedToCorrectLocation()
        {
            _sqs.Received().SendMessage(Arg.Is<SendMessageRequest>(x => x.QueueUrl == Url));
        }
    }
}
