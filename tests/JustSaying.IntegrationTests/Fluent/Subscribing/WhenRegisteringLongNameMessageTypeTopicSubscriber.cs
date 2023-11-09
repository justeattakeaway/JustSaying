using System.Net;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class WhenRegisteringLongNameMessageTypeTopicSubscriber(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_A_Queue_Is_Created()
    {
        // Arrange
        var completionSource = new TaskCompletionSource<object>();
        var handler = CreateHandler<LongestPossibleMessageSizeLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongMessag>(completionSource);

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackTopic<LongestPossibleMessageSizeLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongMessag>(UniqueName))
            .AddSingleton(handler);

        await WhenAsync(
            services,
            async (publisher, listener, serviceProvider, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(new LongestPossibleMessageSizeLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongMessag(), cancellationToken);
                completionSource.Task.Wait(cancellationToken);

                // Assert
                var busBuilder = serviceProvider.GetRequiredService<MessagingBusBuilder>();
                var clientFactory = busBuilder.BuildClientFactory();

                var client = clientFactory.GetSqsClient(Region);

                var response = await client.GetQueueUrlAsync(UniqueName, cancellationToken).ConfigureAwait(false);

                response.ShouldNotBeNull();
                response.HttpStatusCode.ShouldBe(HttpStatusCode.OK);
                response.QueueUrl.ShouldNotBeNull();
            });
    }

    public sealed class LongestPossibleMessageSizeLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongLongMessag : Message
    {
    }
}