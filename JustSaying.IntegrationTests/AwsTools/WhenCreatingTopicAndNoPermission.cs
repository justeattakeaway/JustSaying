using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
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
            var snsClient = new NoTopicCreationAwsClientFactory().GetSnsClient(Region);

            _topic = new SnsTopicByName(
                UniqueName,
                snsClient,
                new MessageSerialisationRegister(new NonGenericMessageSubjectProvider()),
                LoggerFactory,
                new NonGenericMessageSubjectProvider());

            _createWasSuccessful = _topic.CreateAsync().GetAwaiter().GetResult();
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
