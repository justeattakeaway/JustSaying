using System;
using Amazon;
using Amazon.SimpleNotificationService.Model;
using JustBehave;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;
using NUnit.Framework;

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

            Configuration = new PublishConfig();

            TestEndpoint = _regionEndpoint;

            DeleteQueueIfItAlreadyExists(_regionEndpoint, _queueName);
            DeleteTopicIfItAlreadyExists(_regionEndpoint, _topicName);
        }

        protected override void When()
        {
            SystemUnderTest.WithSqsTopicSubscriber()
            .IntoQueue(_queueName)
            .ConfigureSubscriptionWith(cfg => cfg.MessageRetentionSeconds = 60)
                .WithMessageHandler(Substitute.For<IHandler<Message>>());
        }

        [Then]
        public void QueueAndTopicAreCreatedAndQueueIsSubscribedToTheTopicWithCorrectPermissions()
        {
            //This is a bad test as we're testing 4 things in 1 test, oh well.

            Topic topic;
            Assert.IsTrue(TryGetTopic(_regionEndpoint, _topicName, out topic), "Topic does not exist");

            string queueUrl;
            Assert.IsTrue(WaitForQueueToExist(_regionEndpoint, _queueName, out queueUrl), "Queue does not exist");

            Assert.IsTrue(IsQueueSubscribedToTopic(_regionEndpoint, topic, queueUrl), "Queue is not subscribed to the topic");

            Assert.IsTrue(QueueHasPolicyForTopic(_regionEndpoint, topic, queueUrl), "Queue does not have a policy for the topic");

        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            DeleteQueueIfItAlreadyExists(_regionEndpoint, _queueName);
            DeleteTopicIfItAlreadyExists(_regionEndpoint, _topicName);
        }
    }
}