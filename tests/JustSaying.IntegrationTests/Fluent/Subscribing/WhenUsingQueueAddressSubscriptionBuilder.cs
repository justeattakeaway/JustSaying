using JustSaying.AwsTools;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class WhenUsingQueueAddressSubscriptionBuilder : IntegrationTestBase
{
    [Test]
    public async Task DoesNotCheckQueueExistenceByDefault()
    {
        // Arrange - a subscription pointing at a queue that does not exist, with no existence check.
        var queueUrl = $"https://sqs.{RegionName}.amazonaws.com/123456789012/{UniqueName}";

        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder =>
                builder.Subscriptions(c => c.ForQueueUrl<SimpleMessage>(queueUrl)))
            .AddJustSayingHandlers(new[] { handler });

        // Act + Assert - starting the listener does not verify the queue exists, so it does not throw.
        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
            });
    }

    [Test]
    public async Task WithQueueExistenceCheckSucceedsWhenQueueExists()
    {
        // Arrange
        IAwsClientFactory clientFactory = CreateClientFactory();
        var sqsClient = clientFactory.GetSqsClient(Region);
        var queueResponse = await sqsClient.CreateQueueAsync(UniqueName);

        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder =>
                builder.Subscriptions(c => c.ForQueueUrl<SimpleMessage>(
                    queueResponse.QueueUrl,
                    configure: queue => queue.WithQueueExistenceCheck())))
            .AddJustSayingHandlers(new[] { handler });

        // Act + Assert - the queue exists, so starting the listener does not throw.
        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
            });
    }

    [Test]
    public async Task WithQueueExistenceCheckThrowsWhenQueueDoesNotExist()
    {
        // Arrange - point at a queue that is never created, but using a URL the simulator understands.
        IAwsClientFactory clientFactory = CreateClientFactory();
        var sqsClient = clientFactory.GetSqsClient(Region);
        var existingQueueResponse = await sqsClient.CreateQueueAsync(UniqueName);

        var missingQueueName = $"{Guid.NewGuid():N}-does-not-exist";
        var missingQueueUrl = existingQueueResponse.QueueUrl.Replace(UniqueName, missingQueueName);

        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder =>
                builder.Subscriptions(c => c.ForQueueUrl<SimpleMessage>(
                    missingQueueUrl,
                    configure: queue => queue.WithQueueExistenceCheck())))
            .AddJustSayingHandlers(new[] { handler });

        using var provider = services.BuildServiceProvider();
        var listener = provider.GetRequiredService<IMessagingBus>();

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => listener.StartAsync(CancellationToken.None));

        // Assert
        exception.Message.ShouldBe($"SQS queue '{missingQueueName}' with URL '{missingQueueUrl}' does not exist.");
    }
}
