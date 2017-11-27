using Amazon;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenCreatingTopicAndNoPermission : WhenCreatingTopicByName
    {
        private SnsTopicByName _topic;

        private bool _createWasSuccessful;

        protected override void When()
        {
            var snsClient = new NoTopicCreationAwsClientFactory().GetSnsClient(RegionEndpoint.EUWest1);
            _topic = new SnsTopicByName(UniqueName, snsClient, new MessageSerialisationRegister(), new LoggerFactory());
            _createWasSuccessful = _topic.Create();
        }

        [Fact]
        public void TopicCreationWasUnsuccessful()
        {
            _createWasSuccessful.ShouldBeFalse();
        }

        [Fact]
        public void FallbackToExistenceCheckStillPopulatesArn()
        {
            _topic.Arn.ShouldNotBeNull();
            _topic.Arn.ShouldEndWith(_topic.TopicName);
            _topic.Arn.ShouldBe(CreatedTopic.Arn);
        }
    }
}
