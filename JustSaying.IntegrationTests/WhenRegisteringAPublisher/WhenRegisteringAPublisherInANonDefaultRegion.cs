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

        protected override async Task Given()
        {
            await base.Given();

            _topicName = "message";

            Configuration = new MessagingConfig();

            await DeleteTopicIfItAlreadyExists(_topicName);
        }

        protected override Task When()
        {
            SystemUnderTest.WithSnsMessagePublisher<Message>();
            return Task.CompletedTask;
        }

        [AwsFact]
        public async Task ASnsTopicIsCreatedInTheNonDefaultRegion()
        {
            bool topicExists;
            (topicExists, _topic) = await TryGetTopic(_topicName);
            topicExists.ShouldBeTrue();
        }

        protected override async Task PostAssertTeardownAsync()
        {
            if (_topic != null)
            {
                await DeleteTopicAsync(_topic);
            }
        }
    }
}
