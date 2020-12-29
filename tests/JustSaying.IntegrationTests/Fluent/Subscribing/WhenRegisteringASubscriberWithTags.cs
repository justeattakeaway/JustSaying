using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS.Model;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenRegisteringASubscriberWithTags : IntegrationTestBase
    {
        private const string QueueName = "simple-message-queue-with-tags";

        public WhenRegisteringASubscriberWithTags(ITestOutputHelper outputHelper) : base(outputHelper)
        { }

        [Fact]
        public async Task Then_A_Queue_For_Topic_Subscription_Is_Created_With_The_Correct_Tags()
        {
            // Arrange
            var tags = new Dictionary<string, string>
            {
                [Guid.NewGuid().ToString()] = null,
                [Guid.NewGuid().ToString()] = "Value"
            };

            await AssertQueueTagsExist(tags,
                builder =>
                    builder.ForTopic<SimpleMessage>((queueBuilder) =>
                    {
                        foreach ((string key, string value) in tags)
                        {
                            queueBuilder.WithTag(key, value);
                        }

                        queueBuilder.WithReadConfiguration(ReadConfig);
                    }));
        }

        [Fact]
        public async Task Then_A_Queue_Subscription_Is_Created_With_The_Correct_Tags()
        {
            // Arrange
            var tags = new Dictionary<string, string>
            {
                [Guid.NewGuid().ToString()] = "Testing-One",
                [Guid.NewGuid().ToString()] = "Testing-Two"
            };

            await AssertQueueTagsExist(tags,
                builder =>
                    builder.ForQueue<SimpleMessage>((queueBuilder) =>
                    {
                        foreach ((string key, string value) in tags)
                        {
                            queueBuilder.WithTag(key, value);
                        }

                        queueBuilder.WithReadConfiguration(ReadConfig);
                    }));
        }

        private async Task AssertQueueTagsExist(Dictionary<string, string> expectedQueueTags, Action<SubscriptionsBuilder> subscriptionBuilder)
        {
            var serviceProvider = GivenJustSaying()
                .ConfigureJustSaying((builder) =>
                    builder.Subscriptions(subscriptionBuilder))
                .AddJustSayingHandler<SimpleMessage, NullOpHandler<SimpleMessage>>()
                .BuildServiceProvider();

            // Act
            var messageBus = serviceProvider.GetRequiredService<IMessagingBus>();
            await messageBus.StartAsync(CancellationToken.None);

            // Assert
            var busBuilder = serviceProvider.GetRequiredService<MessagingBusBuilder>();
            var clientFactory = busBuilder.BuildClientFactory();

            var client = clientFactory.GetSqsClient(RegionEndpoint.EUWest1);

            var queueUrl = (await client.GetQueueUrlAsync(QueueName)).QueueUrl;

            var queueTags = (await client.ListQueueTagsAsync(new ListQueueTagsRequest { QueueUrl = queueUrl })).Tags;

            foreach ((string key, string value) in expectedQueueTags)
            {
                queueTags.ShouldContain((t) => t.Key == key && t.Value == CleanTagValue(value));
            }
        }

        private static void ReadConfig(SqsReadConfiguration readConfig) => readConfig.QueueName = QueueName;

        private static string CleanTagValue(string tagValue) => string.IsNullOrEmpty(tagValue) ? null : tagValue;
    }
}
