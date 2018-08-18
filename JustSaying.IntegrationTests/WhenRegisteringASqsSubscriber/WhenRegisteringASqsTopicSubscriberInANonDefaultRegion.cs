using System;
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
            _queueName = "queue" + DateTime.Now.Ticks;

            EnableMockedBus();

            Configuration = new MessagingConfig();

            DeleteQueueIfItAlreadyExists(TestEndpoint, _queueName).Wait();
            DeleteTopicIfItAlreadyExists(TestEndpoint, _topicName).Wait();
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
            var (topicExists, topic) = await TryGetTopic(TestEndpoint, _topicName);
            Assert.True(topicExists, "Topic does not exist");

            var (queueExists, queueUrl) = await WaitForQueueToExist(TestEndpoint, _queueName);
            Assert.True(queueExists, "Queue does not exist");

            Assert.True(await IsQueueSubscribedToTopic(TestEndpoint, topic, queueUrl), "Queue is not subscribed to the topic");

            Assert.True(await QueueHasPolicyForTopic(TestEndpoint, topic, queueUrl), "Queue does not have a policy for the topic");
        }
        
        protected override void PostAssertTeardown()
        {
            DeleteQueueIfItAlreadyExists(TestEndpoint, _queueName).Wait();
            DeleteTopicIfItAlreadyExists(TestEndpoint, _topicName).Wait();
        }
    }
}
