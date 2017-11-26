using System;
using System.Threading.Tasks;
using Amazon;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;
using Xunit;
using Assert = Xunit.Assert;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    public class WhenRegisteringASqsTopicSubscriberInANonDefaultRegion : FluentNotificationStackTestBase
    {
        private string _topicName;
        private string _queueName;
        private RegionEndpoint _regionEndpoint;

        protected override void Given()
        {
            _topicName = "message";
            _queueName = "queue" + DateTime.Now.Ticks;
            _regionEndpoint = RegionEndpoint.SAEast1;

            EnableMockedBus();

            Configuration = new MessagingConfig();

            TestEndpoint = _regionEndpoint;

            DeleteQueueIfItAlreadyExists(_regionEndpoint, _queueName).Wait();
            DeleteTopicIfItAlreadyExists(_regionEndpoint, _topicName).Wait();
        }

        protected override Task When()
        {
            SystemUnderTest.WithSqsTopicSubscriber()
            .IntoQueue(_queueName)
            .ConfigureSubscriptionWith(cfg => cfg.MessageRetentionSeconds = 60)
                .WithMessageHandler(Substitute.For<IHandlerAsync<Message>>());

            return Task.CompletedTask;
        }

        [Fact]
        public async Task QueueAndTopicAreCreatedAndQueueIsSubscribedToTheTopicWithCorrectPermissions()
        {
            //This is a bad test as we're testing 4 things in 1 test, oh well.
            
            var (topicExists, topic) = await TryGetTopic(_regionEndpoint, _topicName);
            Assert.True(topicExists, "Topic does not exist");

            var (queueExists, queueUrl) = await WaitForQueueToExist(_regionEndpoint, _queueName);
            Assert.True(queueExists, "Queue does not exist");

            Assert.True(await IsQueueSubscribedToTopic(_regionEndpoint, topic, queueUrl), "Queue is not subscribed to the topic");

            Assert.True(await QueueHasPolicyForTopic(_regionEndpoint, topic, queueUrl), "Queue does not have a policy for the topic");

        }
        
        protected override void PostAssertTeardown()
        {
            DeleteQueueIfItAlreadyExists(_regionEndpoint, _queueName).Wait();
            DeleteTopicIfItAlreadyExists(_regionEndpoint, _topicName).Wait();
        }
    }
}
