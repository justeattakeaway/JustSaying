using System.Collections.Generic;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.TestingFramework;
using Newtonsoft.Json;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.Sqs
{
    public class WhenPublishing : BehaviourTest<SqsPublisher>
    {
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private const string Url = "https://blablabla/" + QueueName;
        private readonly GenericMessage _message = new GenericMessage {Content = "Hello"};
        private const string QueueName = "queuename";

        protected override SqsPublisher CreateSystemUnderTest()
        {
            return new SqsPublisher(QueueName, _sqs, 0);
        }

        protected override void Given()
        {
            _sqs.ListQueues(Arg.Any<ListQueuesRequest>()).Returns(new ListQueuesResponse{QueueUrls = new List<string>{Url}});
            _sqs.GetQueueAttributes(Arg.Any<GetQueueAttributesRequest>()).Returns(new GetQueueAttributesResponse());
        }

        protected override void When()
        {
            SystemUnderTest.Publish("GenericMessage", JsonConvert.SerializeObject(_message));
        }

        [Then]
        public void MessageIsPublishedToQueueWithCorrectContent()
        {
            _sqs.Received().SendMessage(Arg.Is<SendMessageRequest>(x => x.MessageBody.Contains("Hello")));
        }

        [Then]
        public void MessageIsPublishedToQueueWithCorrectId()
        {
            _sqs.Received().SendMessage(Arg.Is<SendMessageRequest>(x => x.MessageBody.Contains(_message.Id.ToString())));
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