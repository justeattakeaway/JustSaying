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
    public class WhenRegisteringAPublisherForMultipleRegions : IntegrationTestBase
    {
        public WhenRegisteringAPublisherForMultipleRegions(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_A_Topic_Is_Created_In_Each_Region()
        {
            // Arrange
            var regions = new RegionEndpoint[]
            {
                RegionEndpoint.EUWest1,
                RegionEndpoint.EUWest2,
            };

            var serviceProvider = GivenJustSaying()
                .ConfigureJustSaying((builder) => builder.WithLoopbackTopic<MyMessageForMultipleRegions>(UniqueName))
                .ConfigureJustSaying((builder) => builder.Messaging((config) => config.WithRegions(regions)))
                .BuildServiceProvider();

            // Act
            var publisher = serviceProvider.GetService<IMessagePublisher>();
            await publisher.StartAsync(CancellationToken.None);

            await publisher.PublishAsync(new MyMessageForMultipleRegions());

            // Assert
            var busBuilder = serviceProvider.GetRequiredService<MessagingBusBuilder>();
            var clientFactory = busBuilder.BuildClientFactory();

            foreach (var region in regions)
            {
                var client = clientFactory.GetSnsClient(region);

                var topics = new List<Topic>();
                string nextToken = null;

                do
                {
                    var topicsResponse = await client.ListTopicsAsync(nextToken).ConfigureAwait(false);
                    nextToken = topicsResponse.NextToken;
                    topics.AddRange(topicsResponse.Topics);
                }
                while (nextToken != null);

                topics
                    .Select((p) => p.TopicArn)
                    .Count((p) => p.EndsWith(":MyMessageForMultipleRegions", StringComparison.OrdinalIgnoreCase))
                    .ShouldBe(1);
            }
        }

        private sealed class MyMessageForMultipleRegions : Message
        {
        }
    }
}
