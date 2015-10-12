using Amazon.SimpleNotificationService.Model;
using JustBehave;
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
            base.Given();

            _topicName = "message";

            Configuration = new PublishConfig();

            DeleteTopicIfItAlreadyExists(TestEndpoint, _topicName);

        }

        protected override void When()
        {
            SystemUnderTest.WithSnsMessagePublisher<Message>();
        }

        [Then]
        public void ASnsTopicIsCreatedInTheNonDefaultRegion()
        {
            Assert.IsTrue(TryGetTopic(TestEndpoint, _topicName, out _topic));
        }

        [TearDown]
        public void TearDown()
        {
            if (_topic != null)
            {
                DeleteTopic(TestEndpoint, _topic);
            }
        }
    }
}