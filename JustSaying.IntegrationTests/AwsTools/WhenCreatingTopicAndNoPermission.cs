using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenCreatingTopicAndNoPermission : WhenCreatingTopicByName
    {
        private SnsTopicByName _topic;
        private bool _createWasSuccessful;

        protected override async Task When()
        {
            var snsClient = new NoTopicCreationAwsClientFactory().GetSnsClient(Region);

            _topic = new SnsTopicByName(
                UniqueName,
                snsClient,
                new MessageSerializationRegister(new NonGenericMessageSubjectProvider()),
                LoggerFactory,
                new NonGenericMessageSubjectProvider());

            _createWasSuccessful = await _topic.CreateAsync();
        }

        [AwsFact]
        public void TopicCreationWasUnsuccessful()
        {
            _createWasSuccessful.ShouldBeFalse();
        }

        [AwsFact]
        public void FallbackToExistenceCheckStillPopulatesArn()
        {
            _topic.Arn.ShouldNotBeNull();
            _topic.Arn.ShouldEndWith(_topic.TopicName);
            _topic.Arn.ShouldBe(CreatedTopic.Arn);
        }
    }
}
