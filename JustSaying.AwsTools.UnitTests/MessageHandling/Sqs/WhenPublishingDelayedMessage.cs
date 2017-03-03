using System.Collections.Generic;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.Sqs
{
    public class WhenPublishingDelayedMessage : BehaviourTest<SqsPublisher>
    {
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private const string Url = "https://blablabla/" + QueueName;
        private readonly DelayedMessage _message = new DelayedMessage(delaySeconds: 1);
        private const string QueueName = "queuename";

        protected override SqsPublisher CreateSystemUnderTest()
        {
            var sqs = new SqsPublisher(RegionEndpoint.EUWest1, QueueName, _sqs, 0,
                _serialisationRegister, Substitute.For<ILoggerFactory>());
            sqs.Exists();
            return sqs;
        }

        protected override void Given()
        {
            _sqs.ListQueues(Arg.Any<ListQueuesRequest>()).Returns(new ListQueuesResponse { QueueUrls = new List<string> { Url } });
            _sqs.GetQueueAttributes(Arg.Any<GetQueueAttributesRequest>()).Returns(new GetQueueAttributesResponse());
        }

        protected override void When()
        {
            SystemUnderTest.Publish(_message);
        }

        [Then]
        public void MessageIsPublishedWithDelaySecondsPropertySet()
        {
            _sqs.Received().SendMessage(Arg.Is<SendMessageRequest>(x => x.DelaySeconds.Equals(1)));
        }
    }
}
