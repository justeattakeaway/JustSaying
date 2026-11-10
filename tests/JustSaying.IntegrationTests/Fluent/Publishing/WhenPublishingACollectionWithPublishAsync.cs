using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

/// <summary>
/// A v8 batch call site, <c>PublishAsync(messages)</c>, still compiles against v9 because a collection
/// is itself a valid single message. It can only fail at runtime, so the error has to say what to do.
/// </summary>
public class WhenPublishingACollectionWithPublishAsync : IntegrationTestBase
{
    [Test]
    public async Task Then_The_Error_Points_To_PublishBatchAsync_For_A_Topic()
    {
        await AssertPublishingACollectionPointsToPublishBatchAsync(
            pub => pub.WithTopic<SimpleMessage>());
    }

    [Test]
    public async Task Then_The_Error_Points_To_PublishBatchAsync_For_A_Dynamic_Topic()
    {
        await AssertPublishingACollectionPointsToPublishBatchAsync(
            pub => pub.WithTopic<SimpleMessage>(c => c.WithTopicName(msg => $"{msg.Tenant}-{UniqueName}")));
    }

    private async Task AssertPublishingACollectionPointsToPublishBatchAsync(Action<PublicationsBuilder> configure)
    {
        // Arrange
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying(builder => builder.Publications(configure))
            .BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        await publisher.StartAsync(CancellationToken.None);

        List<SimpleMessage> messages = [new SimpleMessage { Tenant = "uk" }, new SimpleMessage { Tenant = "uk" }];

        // Act and Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => publisher.PublishAsync(messages));
        exception.Message.ShouldContain("PublishBatchAsync");
    }
}
