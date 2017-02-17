using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon;
using Amazon.SimpleNotificationService.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    public class WhenRegisteringASqsTopicSubscriberWithPrecachedTopicsEnabled : FluentNotificationStackTestBase
    {
        private string _topicName;
        private string _queueName;
        private RegionEndpoint _regionEndpoint;
        private const int AwsTopicPageSizePlusOne = 101;
        private readonly List<string> _topicsCreatedForTest = new List<string>(AwsTopicPageSizePlusOne);
        protected override void Given()
        {
            _topicName = "message";
            _queueName = "queue" + DateTime.Now.Ticks;
            _regionEndpoint = RegionEndpoint.SAEast1;

            EnableMockedBus();

            Configuration = new MessagingConfig();

            TestEndpoint = _regionEndpoint;

            DeleteQueueIfItAlreadyExists(_regionEndpoint, _queueName);


            DeleteTopicIfItAlreadyExists(_regionEndpoint, _topicName);

            var client = CreateMeABus.DefaultClientFactory().GetSnsClient(_regionEndpoint);
            for (int i = 0; i < AwsTopicPageSizePlusOne; i++)
            {
                var topicName = "TESTTOPIC-" + i;
                _topicsCreatedForTest.Add(topicName);
                client.CreateTopic(new CreateTopicRequest {Name = topicName });
            }
        }

        protected override void When()
        {
            SystemUnderTest
                .PreloadTopics()
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
        public void TopicsWereStoredInRegionalTopicCache()
        {
            var queueCache = ExtractTopicCacheFromPrivateFieldWithoutRefactoringJustSaying();

            Assert.That(queueCache.Count,Is.EqualTo(1));
            Assert.That(queueCache.First().Key.Count, Is.EqualTo(101));
        }

        private RegionResourceCache<SnsTopicByName> ExtractTopicCacheFromPrivateFieldWithoutRefactoringJustSaying()
        {
            var queueCreatorField = SystemUnderTest.GetType().GetField("_amazonQueueCreator", BindingFlags.Instance | BindingFlags.NonPublic);
            var queueCreator = queueCreatorField.GetValue(SystemUnderTest) as IVerifyAmazonQueues;

            var queueCacheField = queueCreator.GetType().GetField("_topicCache", BindingFlags.Instance | BindingFlags.NonPublic);
            var queueCache = queueCacheField.GetValue(queueCreator) as RegionResourceCache<SnsTopicByName>;
            return queueCache;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            DeleteQueueIfItAlreadyExists(_regionEndpoint, _queueName);
            DeleteTopicIfItAlreadyExists(_regionEndpoint, _topicName);
            foreach (var topic in _topicsCreatedForTest)
            {
                DeleteTopicIfItAlreadyExists(_regionEndpoint, topic);
            }
        }
    }
}
