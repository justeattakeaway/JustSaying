using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenRegisteringASqsTopicSubscriberForMultipleRegions : IntegrationTestBase
    {
        public WhenRegisteringASqsTopicSubscriberForMultipleRegions(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_A_Queue_Is_Created()
        {
            // Arrange
            var regions = new RegionEndpoint[]
            {
                RegionEndpoint.EUWest1,
                RegionEndpoint.EUWest2,
            };

            var completionSource = new TaskCompletionSource<object>();
            var handler = CreateHandler<MySqsTopicMessageForMultipleRegions>(completionSource);

            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) => builder.WithLoopbackTopic<MySqsTopicMessageForMultipleRegions>(UniqueName))
                .ConfigureJustSaying((builder) => builder.Messaging((config) => config.WithRegions(regions)))
                .AddSingleton(handler);

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    listener.Start(cancellationToken);

                    // Act
                    await publisher.PublishAsync(new MySqsTopicMessageForMultipleRegions(), cancellationToken);
                    completionSource.Task.Wait(cancellationToken);

                    // Assert
                    var busBuilder = serviceProvider.GetRequiredService<MessagingBusBuilder>();
                    var clientFactory = busBuilder.BuildClientFactory();

                    foreach (var region in regions)
                    {
                        var sqsClient = clientFactory.GetSqsClient(region);

                        var response = await sqsClient.GetQueueUrlAsync(UniqueName).ConfigureAwait(false);

                        response.ShouldNotBeNull();
                        response.HttpStatusCode.ShouldBe(HttpStatusCode.OK);
                        response.QueueUrl.ShouldNotBeNull();

                        var snsClient = clientFactory.GetSnsClient(region);

                        var topics = new List<Topic>();
                        string nextToken = null;

                        do
                        {
                            var topicsResponse = await snsClient.ListTopicsAsync(nextToken).ConfigureAwait(false);
                            nextToken = topicsResponse.NextToken;
                            topics.AddRange(topicsResponse.Topics);
                        }
                        while (nextToken != null);

                        topics
                            .Select((p) => p.TopicArn)
                            .Count((p) => p.EndsWith(":MySqsTopicMessageForMultipleRegions", StringComparison.OrdinalIgnoreCase))
                            .ShouldBe(1);
                    }
                });
        }

        public class MySqsTopicMessageForMultipleRegions : JustSaying.Models.Message
        {
        }
    }
}
