using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    public class WhenCreatingTopicThatExistsAlready : WhenCreatingTopicByName
    {
        private bool _createWasSuccessful;
        private SnsTopicByName _topic;

        protected override void When()
        {
            _topic = new SnsTopicByName(UniqueName, Bus, new MessageSerialisationRegister(), new LoggerFactory());
            _createWasSuccessful = _topic.Create();
        }

        [Fact]
        public void CreateCallIsStillSuccessful()
        {
            _createWasSuccessful.ShouldBeTrue();
        }

        [Fact]
        public void TopicArnIsPopulated()
        {
            _topic.Arn.ShouldNotBeNull();
            _topic.Arn.ShouldEndWith(_topic.TopicName);
            _topic.Arn.ShouldBe(CreatedTopic.Arn);
        }

    }
}
