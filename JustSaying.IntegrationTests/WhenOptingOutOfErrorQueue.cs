using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NUnit.Framework;
using Microsoft.Extensions.Logging;

namespace JustSaying.IntegrationTests
{
    public class OrderPlacedHandler : IHandlerAsync<GenericMessage>
    {
        public Task<bool> Handle(GenericMessage message) => Task.FromResult(true);
    }

    public class WhenOptingOutOfErrorQueue
    {
        private IAmazonSQS _client;

        [SetUp]
        public void SetUp()
        {
            _client = CreateMeABus.DefaultClientFactory().GetSqsClient(RegionEndpoint.EUWest1);
        }

        [Test]
        public async Task ErrorQueueShouldNotBeCreated()
        {
            var queueName = "test-queue-issue-191";
#pragma warning disable CS0618 // Type or member is obsolete
            await CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion("eu-west-1")
                .WithSnsMessagePublisher<GenericMessage>()

                .WithSqsTopicSubscriber()
                .IntoQueue(queueName)
                .ConfigureSubscriptionWith(policy =>
                {
                    policy.ErrorQueueOptOut = true;
                })
                .WithMessageHandler(new OrderPlacedHandler())
                .BuildBusAsync();
#pragma warning restore CS0618 // Type or member is obsolete

            await AssertThatQueueDoesNotExist(queueName+ "_error");
        }

        private async Task AssertThatQueueDoesNotExist(string name)
        {
            var sqsQueueByName = new SqsQueueByName(RegionEndpoint.EUWest1, name, _client, 1, new LoggerFactory());
            Assert.IsFalse(await sqsQueueByName.ExistsAsync(), $"Expecting queue '{name}' to not exist but it does.");
        }
    }
}
