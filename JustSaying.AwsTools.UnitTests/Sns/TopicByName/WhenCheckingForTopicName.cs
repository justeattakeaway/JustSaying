using Amazon.SimpleNotificationService;
using JustEat.Testing;
using JustSaying.Messaging.MessageSerialisation;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.Sns.TopicByName
{
    public class WhenCheckingForTopicName : BehaviourTest<SnsTopicByArn>
    {
        protected override void Given()
        {

        }
        protected override void When()
        {

        }

        protected override SnsTopicByArn CreateSystemUnderTest()
        {
            return new SnsTopicByArn("arn:aws:sqs:eu-west-1:507204202721:be-qa5-emailsender-orderresolved", Substitute.For<IAmazonSimpleNotificationService>(), Substitute.For<IMessageSerialisationRegister>());
        }

        [Then]
        public void NonMatchingTopicNameDoesNotMatch()
        {
            Assert.IsFalse(SystemUnderTest.Matches("be-qa5-emailsender-orderresolved2"));
        }

        [Then]
        public void NonMathcingTopicDoesNotMatch()
        {
            Assert.IsFalse(SystemUnderTest.Matches("be-qa5-emailsender-order"));
        }

        [Then]
        public void OnlyExactTopicMatches()
        {
            Assert.IsTrue(SystemUnderTest.Matches("be-qa5-emailsender-orderresolved"));
        }
    }
}