using Amazon;
using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenRegisteringAPublisherForRegion(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_A_Topic_Is_Created_In_That_Region()
    {
        await ThenATopicIsCreatedInThatRegion<IMessagePublisher>();
        await ThenATopicIsCreatedInThatRegion<IMessageBatchPublisher>();
    }

    private async Task ThenATopicIsCreatedInThatRegion<T>()
        where T : IStartable
    {
        // Arrange
        var region = RegionEndpoint.EUWest1;

        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
                builder.WithLoopbackTopic<MyMessageForRegion>(UniqueName))
            .ConfigureJustSaying((builder) => builder.Messaging((config) => config.WithRegion(region)))
            .BuildServiceProvider();

        // Act
        using var source = new CancellationTokenSource(Timeout);
        var publisher = serviceProvider.GetRequiredService<T>();
        await publisher.StartAsync(source.Token);

        // Assert
        var busBuilder = serviceProvider.GetRequiredService<MessagingBusBuilder>();
        var clientFactory = busBuilder.BuildClientFactory();

        var client = clientFactory.GetSnsClient(region);

        (await client.GetAllTopics())
            .Select((p) => p.TopicArn)
            .Count((p) => p.EndsWith($":{nameof(MyMessageForRegion)}", StringComparison.OrdinalIgnoreCase))
            .ShouldBe(1);
    }

    private sealed class MyMessageForRegion : Message
    {
    }
}
