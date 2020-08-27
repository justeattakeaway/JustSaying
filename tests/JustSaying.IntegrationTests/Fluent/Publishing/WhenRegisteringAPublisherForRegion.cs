using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Publishing
{
    public class WhenRegisteringAPublisherForRegion : IntegrationTestBase
    {
        public WhenRegisteringAPublisherForRegion(ITestOutputHelper outputHelper)
            : base(outputHelper)
        { }

        [AwsFact]
        public async Task Then_A_Topic_Is_Created_In_That_Region()
        {
            // Arrange
            var region = RegionEndpoint.EUWest1;

            var serviceProvider = GivenJustSaying()
                .ConfigureJustSaying((builder) =>
                    builder.WithLoopbackTopic<MyMessageForRegion>(UniqueName))
                .ConfigureJustSaying((builder) => builder.Messaging((config) => config.WithRegion(region)))
                .BuildServiceProvider();

            // Act
            var publisher = serviceProvider.GetService<IMessagePublisher>();
            await publisher.StartAsync(CancellationToken.None);

            await publisher.PublishAsync(new MyMessageForRegion());

            // Assert
            var busBuilder = serviceProvider.GetRequiredService<MessagingBusBuilder>();
            var clientFactory = busBuilder.BuildClientFactory();

            var client = clientFactory.GetSnsClient(region);

            var topics = new List<Topic>();
            string nextToken = null;

            do
            {
                var topicsResponse = await client.ListTopicsAsync(nextToken).ConfigureAwait(false);
                nextToken = topicsResponse.NextToken;
                topics.AddRange(topicsResponse.Topics);
            } while (nextToken != null);

            topics
                .Select((p) => p.TopicArn)
                .Count((p) => p.EndsWith($":{nameof(MyMessageForRegion)}", StringComparison.OrdinalIgnoreCase))
                .ShouldBe(1);
        }

        private sealed class MyMessageForRegion : Message
        { }
    }
}
