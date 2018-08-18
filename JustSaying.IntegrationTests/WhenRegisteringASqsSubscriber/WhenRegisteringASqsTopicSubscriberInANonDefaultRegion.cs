using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenRegisteringASqsTopicSubscriberInANonDefaultRegion : FluentNotificationStackTestBase
    {
        private string _topicName;
        private string _queueName;

        protected override void Given()
        {
            base.Given();

            _topicName = "message";
            _queueName = TestFixture.UniqueName;

            EnableMockedBus();

            Configuration = new MessagingConfig();

            DeleteQueueIfItAlreadyExists(_queueName).ResultSync();
            DeleteTopicIfItAlreadyExists(_topicName).ResultSync();
        }

        protected override Task When()
        {
            SystemUnderTest
                .WithSqsTopicSubscriber()
                .IntoQueue(_queueName)
                .ConfigureSubscriptionWith(cfg => cfg.MessageRetentionSeconds = 60)
                .WithMessageHandler(Substitute.For<IHandlerAsync<Message>>());

            return Task.CompletedTask;
        }

        [NotSimulatorFact] // This doesn't appear to work in GoAws
        public async Task QueueAndTopicAreCreatedAndQueueIsSubscribedToTheTopicWithCorrectPermissions()
        {            
            var (topicExists, topic) = await TryGetTopic(_topicName);
            Assert.True(topicExists, "Topic does not exist");

            var (queueExists, queueUrl) = await WaitForQueueToExist(_queueName);
            Assert.True(queueExists, "Queue does not exist");

            Assert.True(await IsQueueSubscribedToTopic(topic, queueUrl), "Queue is not subscribed to the topic");

            Assert.True(await QueueHasPolicyForTopic(topic, queueUrl), "Queue does not have a policy for the topic");
        }
        
        protected override void PostAssertTeardown()
        {
            DeleteQueueIfItAlreadyExists(_queueName).ResultSync();
            DeleteTopicIfItAlreadyExists(_topicName).ResultSync();
        }
    }
}
