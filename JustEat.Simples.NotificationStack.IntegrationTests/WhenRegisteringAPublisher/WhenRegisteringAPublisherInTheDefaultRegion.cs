using Amazon;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging.Messages;
using JustSaying.Stack;
using JustEat.Testing;
using NUnit.Framework;

namespace NotificationStack.IntegrationTests.WhenRegisteringAPublisher
{
    public class WhenRegisteringAPublisherInTheDefaultRegion : FluentNotificationStackTestBase
    {
        private string _topicName;
        private RegionEndpoint _regionEndpoint;
        private Topic _topic;
       
        protected override void Given()
        {
            _regionEndpoint = RegionEndpoint.EUWest1;
            _topicName = "DefaultRegionTestTopic";

            Configuration = new MessagingConfig
            {
                Component = "integrationtestcomponent",
                Environment = "integrationtest",
                Tenant = "all",
                Region = null
            };

            DeleteTopicIfItAlreadyExists(_regionEndpoint, _topicName);
        }

        protected override void When()
        {
            SystemUnderTest.WithSnsMessagePublisher<Message>(_topicName);
        }

        [Then]
        public void ASnsTopicIsCreatedInTheDefaultRegion()
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
    }
}