using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageSerialisation;
using JustBehave;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace AwsTools.UnitTests.Sns.TopicByName
{
    public class WhenPublishing : BehaviourTest<SnsPublisher>
    {
        private const string Message = "the_message_in_json";
        private readonly IAmazonSimpleNotificationService _sns = Substitute.For<IAmazonSimpleNotificationService>();
        private const string TopicName = "topicname";
        private const string TopicArn = "topicarn";

        protected override SnsPublisher CreateSystemUnderTest()
        {
            return new SnsPublisher(TopicName, _sns);
        }

        protected override void Given()
        {
            _sns.FindTopic(TopicName).Returns(new Topic { TopicArn = TopicArn });
        }

        protected override void When()
        {
            SystemUnderTest.Publish("GenericMessage", Message);
        }

        [Then]
        public void MessageIsPublishedToSnsTopic()
        {
            _sns.Received().Publish(Arg.Is<PublishRequest>(x => x.Message == Message));
        }

        [Then]
        public void MessageSubjectIsObjectType()
        {
            _sns.Received().Publish(Arg.Is<PublishRequest>(x => x.Subject == typeof(GenericMessage).Name));
        }

        [Then]
        public void MessageIsPublishedToCorrectLocation()
        {
            _sns.Received().Publish(Arg.Is<PublishRequest>(x => x.TopicArn == TopicArn));
        }
    }
}
