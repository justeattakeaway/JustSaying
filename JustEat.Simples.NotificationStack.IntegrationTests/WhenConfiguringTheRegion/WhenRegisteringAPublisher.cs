using System;
using System.Linq;
using Amazon;
using Amazon.SimpleNotificationService.Model;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace NotificationStack.IntegrationTests.WhenConfiguringTheRegion
{
    public class WhenRegisteringAPublisherInANonDefaultRegion : BehaviourTest<FluentNotificationStack>
    {
        private readonly INotificationStack _stack = Substitute.For<INotificationStack>();
        private string _topicName;
        private RegionEndpoint _regionEndpoint;
        private Topic _topic;

        protected override FluentNotificationStack CreateSystemUnderTest()
        {
            return new FluentNotificationStack(_stack, null);
        }

        protected override void Given()
        {
            _regionEndpoint = RegionEndpoint.USEast1;
            _topicName = "NonDefaultRegionTestTopic";

            _stack.Config.Returns(new MessagingConfig
            {
                Environment = "integrationtest",
                Tenant = "all",
                Region = _regionEndpoint.SystemName
            });

            DeleteTopicIfItAlreadyExists(_regionEndpoint, _topicName);

        }

        protected override void When()
        {
            SystemUnderTest.WithSnsMessagePublisher<Message>(_topicName);
        }

        [Then]
        public void ASnsTopicIsCreatedInTheNonDefaultRegion()
        {
            Assert.IsTrue(TryGetTopic(_regionEndpoint, _topicName, out _topic));
        }

        [TearDown]
        public void TearDown()
        {
            if (_topic != null)
            {
                DeleteTopic(_regionEndpoint, _topic);
            }
        }

        public static void DeleteTopicIfItAlreadyExists(RegionEndpoint regionEndpoint, string topicName)
        {
            Topic topic;
            if (TryGetTopic(regionEndpoint, topicName, out topic))
            {
                DeleteTopic(regionEndpoint, topic);
            }

            if (TryGetTopic(regionEndpoint, topicName, out topic))
            {
                throw new Exception("Deleted topic still exists!");
            }

        }

        public static void DeleteTopic(RegionEndpoint regionEndpoint, Topic topic)
        {
            var client = AWSClientFactory.CreateAmazonSNSClient(regionEndpoint);
            client.DeleteTopic(new DeleteTopicRequest { TopicArn = topic.TopicArn });
        }

        public static
            bool TryGetTopic(RegionEndpoint regionEndpoint, string topicName, out Topic topic)
        {
            var client = AWSClientFactory.CreateAmazonSNSClient(regionEndpoint);
            var topics = client.ListTopics(new ListTopicsRequest());

            topic = topics.ListTopicsResult.Topics.SingleOrDefault(x => x.TopicArn.IndexOf(topicName, StringComparison.InvariantCultureIgnoreCase) >= 0);

            return topic != null;
        }

    }
}