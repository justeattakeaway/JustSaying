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
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private const string Url = "https://blablabla/" + QueueName;
        private readonly GenericMessage _message = new GenericMessage {Content = "Hello"};
        private const string QueueName = "queuename";

        protected override SqsPublisher CreateSystemUnderTest()
        {
            return new SqsPublisher(QueueName, _sqs, 0, _serialisationRegister);
        }

        protected override void Given()
        {
            // ToDo: We need to clean up serialisation (away from Json.Net)
            //var serialiser = Substitute.For<IMessageSerialiser<GenericMessage>>();
            //_serialisationRegister.GeTypeSerialiser(typeof(GenericMessage)).Returns(serialiser);
            _sqs.ListQueues(Arg.Any<ListQueuesRequest>()).Returns(new ListQueuesResponse{QueueUrls = new List<string>{Url}});
            _sqs.GetQueueAttributes(Arg.Any<GetQueueAttributesRequest>()).Returns(new GetQueueAttributesResponse());
        }

        protected override void When()
        {
            SystemUnderTest.Publish(_message);
        }

        [Then]
        public void MessageIsPublishedToQueue()
        {
            // ToDo: Can be better...
            _sqs.Received().SendMessage(Arg.Is<SendMessageRequest>(x => x.MessageBody.Contains("\"Message\":{\"Content\":\"Hello\",\"Id\":\"" + _message.Id)));
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