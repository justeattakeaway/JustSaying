using Amazon.SimpleNotificationService.Model;
using JustEat.Testing;
using JustSaying.Messaging.Messages;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringAPublisher
{
    public class WhenRegisteringAPublisherInANonDefaultRegion : FluentNotificationStackTestBase
    {
        private string _topicName;
        private Topic _topic;

        protected override void Given()
        {
            _topicName = "NonDefaultRegionTestTopic";

            Configuration = new MessagingConfig
            {
                Region = DefaultRegion.SystemName
            };

            DeleteTopicIfItAlreadyExists(DefaultRegion, _topicName);

        }

        protected override void When()
        {
            SystemUnderTest.WithSnsMessagePublisher<Message>(_topicName);
        }

        [Then]
        public void ASnsTopicIsCreatedInTheNonDefaultRegion()
        {
            Assert.IsTrue(TryGetTopic(DefaultRegion, _topicName, out _topic));
        }

        [TearDown]
        public void TearDown()
        {
            if (_topic != null)
            {
                DeleteTopic(DefaultRegion, _topic);
            }
        }
    }
}