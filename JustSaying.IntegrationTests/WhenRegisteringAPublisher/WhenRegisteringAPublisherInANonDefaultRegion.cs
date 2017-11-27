using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Models;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringAPublisher
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenRegisteringAPublisherInANonDefaultRegion : FluentNotificationStackTestBase
    {
        private string _topicName;
        private Topic _topic;

        protected override void Given()
        {
            base.Given();

            _topicName = "message";

            Configuration = new MessagingConfig();

            DeleteTopicIfItAlreadyExists(TestEndpoint, _topicName).Wait();

        }

        protected override Task When()
        {
            SystemUnderTest.WithSnsMessagePublisher<Message>();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task ASnsTopicIsCreatedInTheNonDefaultRegion()
        {
            bool topicExists;
            (topicExists, _topic) = await TryGetTopic(TestEndpoint, _topicName);
            topicExists.ShouldBeTrue();
        }

        protected override void Teardown()
        {
            if (_topic != null)
            {
                DeleteTopic(TestEndpoint, _topic).Wait();
            }
        }
    }
}
