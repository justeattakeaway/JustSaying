using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageSerialisation;
using JustBehave;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace AwsTools.UnitTests.Sns.TopicByArn
{
    public class WhenPublishing : BehaviourTest<SnsTopicByArn>
    {
        private const string Message = "the_message_in_json";
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IAmazonSimpleNotificationService _sns = Substitute.For<IAmazonSimpleNotificationService>();
        private const string Arn = "arn";

        protected override SnsTopicByArn CreateSystemUnderTest()
        {
            return new SnsTopicByArn(Arn, _sns, _serialisationRegister);
        }

        protected override void Given()
        {
            var serialiser = Substitute.For<IMessageSerialiser<GenericMessage>>();
            serialiser.Serialise(Arg.Any<Message>()).Returns(Message);
            _serialisationRegister.GetSerialiser(typeof(GenericMessage)).Returns(serialiser);
        }

        protected override void When()
        {
            SystemUnderTest.Publish(new GenericMessage());
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
            _sns.Received().Publish(Arg.Is<PublishRequest>(x => x.TopicArn == Arn));
        }
    }
}
