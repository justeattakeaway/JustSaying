using System.Collections.Generic;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.Sqs
{
    public class WhenPublishing : BehaviourTest<SqsPublisher>
    {
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly ISqsClient _sqs = Substitute.For<ISqsClient>();
        private const string Url = "https://blablabla/" + QueueName;
        private readonly GenericMessage _message = new GenericMessage {Content = "Hello"};
        private const string QueueName = "queuename";
        private const string SerialisedMessageBody = "<serialized message contents>";

        protected override SqsPublisher CreateSystemUnderTest()
        {
            return new SqsPublisher(QueueName, _sqs, 0, _serialisationRegister);
        }

        protected override void Given()
        {
            var messageSerializer = Substitute.For<IMessageSerialiser>();
            messageSerializer.Serialise(_message).Returns(SerialisedMessageBody);

            _sqs.ListQueues(Arg.Any<ListQueuesRequest>()).Returns(new ListQueuesResponse{QueueUrls = new List<string>{Url}});
            _sqs.GetQueueAttributes(Arg.Any<GetQueueAttributesRequest>()).Returns(new GetQueueAttributesResponse());
            _serialisationRegister.GeTypeSerialiser(typeof(GenericMessage)).Returns(new TypeSerialiser(typeof(GenericMessage), messageSerializer));
        }

        protected override void When()
        {
            SystemUnderTest.Publish(_message);
        }

        [Then]
        public void MessageIsPublishedToQueue()
        {
            // ToDo: Can be better...
            _sqs.Received().SendMessage(Arg.Is<SendMessageRequest>(x => x.MessageBody.Contains("{\"Subject\":\"GenericMessage\",\"Message\":\"<serialized message contents>\"}")));
        }

        [Then]
        public void MessageSubjectIsObjectType()
        {
            // ToDo: Can be better...
            _sqs.Received().SendMessage(Arg.Is<SendMessageRequest>(x => x.MessageBody.Contains("\"Subject\":\"GenericMessage\"")));
        }

        [Then]
        public void MessageIsPublishedToCorrectLocation()
        {
            _sqs.Received().SendMessage(Arg.Is<SendMessageRequest>(x => x.QueueUrl == Url));
        }
    }
}