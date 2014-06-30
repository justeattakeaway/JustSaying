using Amazon.SimpleNotificationService.Model;
using JustEat.Testing;
using JustSaying.Models;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringAPublisher
{
    public class WhenRegisteringAPublisherInANonDefaultRegion : FluentNotificationStackTestBase
    {
        private string _topicName;
        private Topic _topic;

        protected override void Given()
        {
            _topicName = "message";

            Configuration = new MessagingConfig
            {
                Region = DefaultRegion.SystemName
            };

            DeleteTopicIfItAlreadyExists(DefaultRegion, _topicName);

        }

        protected override void When()
        {
            SystemUnderTest.WithSnsMessagePublisher<Message>();
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