using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenCreatingTopicThatExistsAlready : WhenCreatingTopicByName
    {
        private bool _createWasSuccessful;
        private SnsTopicByName _topic;

        protected override void When()
        {
            _topic = new SnsTopicByName(UniqueName, Bus, new MessageSerialisationRegister());
            _createWasSuccessful = _topic.Create();
        }

        [Test]
        public void CreateCallIsStillSuccessful()
        {
            Assert.True(_createWasSuccessful);
        }

        [Test]
        public void TopicArnIsPopulated()
        {
            Assert.That(_topic.Arn, Is.Not.Null);
            Assert.That(_topic.Arn.EndsWith(_topic.TopicName));
            Assert.That(_topic.Arn, Is.EqualTo(CreatedTopic.Arn));
        }

    }
}
