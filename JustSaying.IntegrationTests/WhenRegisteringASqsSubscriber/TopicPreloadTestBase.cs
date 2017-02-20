using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    public abstract class TopicPreloadTestBase : FluentNotificationStackTestBase
    {
        protected string _topicName;
        protected string _queueName;
        protected RegionEndpoint _regionEndpoint;
        protected const int ANumberLargerThanAWSPageSize = 101;
        private readonly List<string> _topicsCreatedForTest = new List<string>(ANumberLargerThanAWSPageSize);
        protected RegionResourceCache<SnsTopicByName> _queueCache;

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

            for (int i = 0; i < ANumberLargerThanAWSPageSize; i++)
            {
                var topicName = "TESTTOPIC" + i;
                _topicsCreatedForTest.Add(topicName);
                client.CreateTopic(new CreateTopicRequest { Name = topicName });
            }
        }


        protected RegionResourceCache<SnsTopicByName> ExtractTopicCacheFromPrivateFieldWithoutRefactoringJustSaying()
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
