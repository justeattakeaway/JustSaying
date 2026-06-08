using JustSaying.AwsTools;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenUsingQueueAddressPublicationBuilder : IntegrationTestBase
{
    [Test]
    public async Task DoesNotCheckQueueExistenceByDefault()
    {
        // Arrange - a publication pointing at a queue that does not exist, with no existence check.
        var queueUrl = $"https://sqs.{RegionName}.amazonaws.com/123456789012/{UniqueName}";

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder =>
                builder.Publications(c => c.WithQueueUrl<SimpleMessage>(queueUrl)));

        using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IMessagePublisher>();

        // Act + Assert - starting the publisher does not verify the queue exists, so it does not throw.
        await publisher.StartAsync(CancellationToken.None);
    }

    [Test]
    public async Task WithQueueExistenceCheckSucceedsWhenQueueExists()
    {
        // Arrange
        IAwsClientFactory clientFactory = CreateClientFactory();
        var sqsClient = clientFactory.GetSqsClient(Region);
        var queueResponse = await sqsClient.CreateQueueAsync(UniqueName);

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder =>
                builder.Publications(c => c.WithQueueUrl<SimpleMessage>(
                    queueResponse.QueueUrl,
                    queue => queue.WithQueueExistenceCheck())));

        using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IMessagePublisher>();

        // Act + Assert - the queue exists, so starting the publisher does not throw.
        await publisher.StartAsync(CancellationToken.None);
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

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder =>
                builder.Publications(c => c.WithQueueUrl<SimpleMessage>(
                    missingQueueUrl,
                    queue => queue.WithQueueExistenceCheck())));

        using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IMessagePublisher>();

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => publisher.StartAsync(CancellationToken.None));

        // Assert
        exception.Message.ShouldBe($"SQS queue '{missingQueueName}' with URL '{missingQueueUrl}' does not exist.");
    }
}
