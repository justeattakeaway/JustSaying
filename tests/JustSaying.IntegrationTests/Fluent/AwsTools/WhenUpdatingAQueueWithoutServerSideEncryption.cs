using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.IntegrationTests;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;

#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.AwsTools;

public class WhenUpdatingAQueueWithoutServerSideEncryption : IntegrationTestBase
{
    [Test]
    public async Task The_Existing_Encryption_Is_Kept()
    {
        // Arrange
        ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();
        IAwsClientFactory clientFactory = CreateClientFactory();

        var client = clientFactory.GetSqsClient(Region);

        var queue = new SqsQueueByName(
            Region,
            UniqueName,
            client,
            1,
            loggerFactory);

        await queue.CreateAsync(
            new SqsBasicConfiguration { ServerSideEncryption = new ServerSideEncryption { KmsMasterKeyId = JustSayingConstants.DefaultSqsAttributeEncryptionKeyId } });

        // Act
        await queue.UpdateQueueAttributeAsync(
            new SqsBasicConfiguration { ServerSideEncryption = null, VisibilityTimeout = TimeSpan.FromSeconds(60) }, CancellationToken.None);

        // Assert
        await queue.ExistsAsync(CancellationToken.None);
        queue.ServerSideEncryption.ShouldNotBeNull();
        queue.ServerSideEncryption.KmsMasterKeyId.ShouldBe(JustSayingConstants.DefaultSqsAttributeEncryptionKeyId);
    }
}
