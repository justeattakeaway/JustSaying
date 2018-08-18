using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenOptingOutOfErrorQueue
    {
        private readonly IAmazonSQS _client;

        public WhenOptingOutOfErrorQueue(ITestOutputHelper outputHelper)
        {
            Region = TestEnvironment.Region;
            LoggerFactory = outputHelper.AsLoggerFactory();
            _client = CreateMeABus.DefaultClientFactory().GetSqsClient(Region);
        }

        private RegionEndpoint Region { get; }

        private ILoggerFactory LoggerFactory { get; }

        [Fact]
        public async Task ErrorQueueShouldNotBeCreated()
        {
            var queueName = "test-queue-issue-191";

            CreateMeABus
                .WithLogging(LoggerFactory)
                .InRegion(Region.SystemName)
                .WithSnsMessagePublisher<SimpleMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue(queueName)
                .ConfigureSubscriptionWith(policy => policy.ErrorQueueOptOut = true)
                .WithMessageHandler(new OrderPlacedHandler());

            await AssertThatQueueDoesNotExist(queueName + "_error");
        }

        private async Task AssertThatQueueDoesNotExist(string name)
        {
            var sqsQueueByName = new SqsQueueByName(Region, name, _client, 1, LoggerFactory);
            (await sqsQueueByName.ExistsAsync()).ShouldBeFalse($"Expecting queue '{name}' to not exist but it does.");
        }
    }
}
