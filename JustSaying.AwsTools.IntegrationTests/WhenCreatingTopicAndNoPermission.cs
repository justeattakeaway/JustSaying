using Amazon;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenCreatingTopicAndNoPermission : WhenCreatingTopicByName
    {
        private SnsTopicByName _topic;

        private bool _createWasSuccessful;

        protected override void When()
        {
            var snsClient = new NoTopicCreationAwsClientFactory().GetSnsClient(RegionEndpoint.EUWest1);
            _topic = new SnsTopicByName(UniqueName, snsClient, new MessageSerialisationRegister());
            _createWasSuccessful = _topic.Create();
        }

        [Test]
        public void TopicCreationWasUnsuccessful()
        {
            Assert.False(_createWasSuccessful);
        }

        [Test]
        public void FallbackToExistenceCheckStillPopulatesArn()
        {
            Assert.That(_topic.Arn, Is.Not.Null);
            Assert.That(_topic.Arn.EndsWith(_topic.TopicName));
            Assert.That(_topic.Arn, Is.EqualTo(CreatedTopic.Arn));
        }
    }
}
