using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.AwsTools;

public class WhenAQueueIsEncrypted : IntegrationTestBase
{
    private const string MasterKeyId = "alias/aws/sqs";

    [Test]
    public async Task Then_The_Error_Queue_Is_Created_With_The_Same_Encryption()
    {
        // Act
        await StartSubscriberAsync(QueueDestination.Named(UniqueName, q => q.WithEncryption(MasterKeyId)));

        // Assert
        var client = CreateClientFactory().GetSqsClient(Region);

        (await GetKmsMasterKeyIdAsync(client, UniqueName)).ShouldBe(MasterKeyId);
        (await GetKmsMasterKeyIdAsync(client, $"{UniqueName}_error")).ShouldBe(MasterKeyId);
    }

    [Test]
    public async Task Then_An_Existing_Error_Queue_Is_Updated_With_The_Same_Encryption()
    {
        // Arrange
        await StartSubscriberAsync(QueueDestination.Named(UniqueName));

        var client = CreateClientFactory().GetSqsClient(Region);
        (await GetKmsMasterKeyIdAsync(client, $"{UniqueName}_error")).ShouldBeNull();

        // Act
        await StartSubscriberAsync(QueueDestination.Named(UniqueName, q => q.WithEncryption(MasterKeyId)));

        // Assert
        (await GetKmsMasterKeyIdAsync(client, UniqueName)).ShouldBe(MasterKeyId);
        (await GetKmsMasterKeyIdAsync(client, $"{UniqueName}_error")).ShouldBe(MasterKeyId);
    }

    [Test]
    public async Task Then_The_Encryption_Is_Not_Removed_By_A_Destination_Without_Encryption()
    {
        // Arrange
        await StartSubscriberAsync(QueueDestination.Named(UniqueName, q => q.WithEncryption(MasterKeyId)));

        // Act
        await StartSubscriberAsync(QueueDestination.Named(UniqueName));

        // Assert
        var client = CreateClientFactory().GetSqsClient(Region);

        (await GetKmsMasterKeyIdAsync(client, UniqueName)).ShouldBe(MasterKeyId);
        (await GetKmsMasterKeyIdAsync(client, $"{UniqueName}_error")).ShouldBe(MasterKeyId);
    }

    private async Task StartSubscriberAsync(QueueDestination destination)
    {
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.Subscriptions((options) => options.ForQueue<SimpleMessage>(destination)))
            .AddSingleton<IHandlerAsync<SimpleMessage>>(new InspectableHandler<SimpleMessage>())
            .BuildServiceProvider();

        using var cts = new CancellationTokenSource(Timeout);
        await serviceProvider.GetRequiredService<IMessagingBus>().StartAsync(cts.Token);
        cts.Cancel();
    }

    private static async Task<string> GetKmsMasterKeyIdAsync(IAmazonSQS client, string queueName)
    {
        var queueUrl = (await client.GetQueueUrlAsync(queueName)).QueueUrl;
        var attributes = await client.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = queueUrl,
            AttributeNames = ["KmsMasterKeyId"],
        });

        return attributes.Attributes?.TryGetValue("KmsMasterKeyId", out var keyId) == true && keyId.Length > 0 ? keyId : null;
    }
}
