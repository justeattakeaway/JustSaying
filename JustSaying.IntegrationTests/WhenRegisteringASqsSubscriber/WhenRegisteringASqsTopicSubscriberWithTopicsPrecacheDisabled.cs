using System.Linq;
using Amazon.SimpleNotificationService.Model;
using JustBehave;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    public class WhenRegisteringASqsTopicSubscriberWithTopicsPrecacheDisabled : TopicPreloadTestBase
    {

        protected override void When()
        {
            _queueCache = ExtractTopicCacheFromPrivateFieldWithoutRefactoringJustSaying();

            SystemUnderTest
                .WithTopicQueryBehaviour(TopicQueryBehaviour.NoPreload)
                .WithSqsTopicSubscriber()
                .IntoQueue(_queueName)
                .ConfigureSubscriptionWith(cfg => cfg.MessageRetentionSeconds = 60)
                .WithMessageHandler(Substitute.For<IHandlerAsync<Message>>());
        }

        [Then]
        public void QueueAndTopicAreCreatedAndQueueIsSubscribedToTheTopicAsExpected()
        {

            Topic topic;
            Assert.IsTrue(TryGetTopic(_regionEndpoint, _topicName, out topic), "Topic does not exist");

            string queueUrl;
            Assert.IsTrue(WaitForQueueToExist(_regionEndpoint, _queueName, out queueUrl), "Queue does not exist");

            Assert.IsTrue(IsQueueSubscribedToTopic(_regionEndpoint, topic, queueUrl), "Queue is not subscribed to the topic");

        }

        [Then]
        public void OnlyTheSubscribedTopicWasStoredInRegionalTopicCache()
        {
            Assert.That(_queueCache.Count, Is.EqualTo(1));
            Assert.That(_queueCache.First().Value.Count, Is.EqualTo(1));

            var testHandlerTopicName = nameof(Message).ToLowerInvariant();
            var cacheEntry = _queueCache.First().Value.SingleOrDefault(x => x.Key == testHandlerTopicName).Value;
            Assert.That(cacheEntry.TopicName, Is.EqualTo(testHandlerTopicName));

        }




    }
}
