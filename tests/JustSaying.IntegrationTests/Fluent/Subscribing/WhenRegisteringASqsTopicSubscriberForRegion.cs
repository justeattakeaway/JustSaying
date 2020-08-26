using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenRegisteringASqsTopicSubscriberForRegion : IntegrationTestBase
    {
        public WhenRegisteringASqsTopicSubscriberForRegion(ITestOutputHelper outputHelper)
            : base(outputHelper)
        { }

        [AwsFact]
        public async Task Then_A_Queue_Is_Created()
        {
            // Arrange
            var region = RegionEndpoint.EUWest2;

            var completionSource = new TaskCompletionSource<object>();
            var handler = CreateHandler<MySqsTopicMessageForRegion>(completionSource);

            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) =>
                    builder.WithLoopbackTopic<MySqsTopicMessageForRegion>(UniqueName))
                .ConfigureJustSaying((builder) => builder.Messaging((config) => config.WithRegion(region)))
                .AddSingleton(handler);

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    await listener.StartAsync(cancellationToken);
                    await publisher.StartAsync(cancellationToken);

                    // Act
                    await publisher.PublishAsync(new MySqsTopicMessageForRegion(),
                        cancellationToken);
                    completionSource.Task.Wait(cancellationToken);

                    // Assert
                    var busBuilder = serviceProvider.GetRequiredService<MessagingBusBuilder>();
                    var clientFactory = busBuilder.BuildClientFactory();

                    var sqsClient = clientFactory.GetSqsClient(region);

                    var response = await sqsClient.GetQueueUrlAsync(UniqueName, cancellationToken)
                        .ConfigureAwait(false);

                    response.ShouldNotBeNull();
                    response.HttpStatusCode.ShouldBe(HttpStatusCode.OK);
                    response.QueueUrl.ShouldNotBeNull();

                    var snsClient = clientFactory.GetSnsClient(region);

                    var topics = new List<Topic>();
                    string nextToken = null;

                    do
                    {
                        var topicsResponse = await snsClient.ListTopicsAsync(nextToken, cancellationToken)
                            .ConfigureAwait(false);
                        nextToken = topicsResponse.NextToken;
                        topics.AddRange(topicsResponse.Topics);
                    } while (nextToken != null);

                    topics
                        .Select((p) => p.TopicArn)
                        .Count((p) => p.EndsWith(":MySqsTopicMessageForMultipleRegions",
                            StringComparison.OrdinalIgnoreCase))
                        .ShouldBe(1);
                });
        }

        public class MySqsTopicMessageForRegion : JustSaying.Models.Message
        { }
    }
}
