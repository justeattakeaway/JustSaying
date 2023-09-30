using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using Microsoft.Extensions.Logging;

#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.AwsTools;

public class WhenCreatingTopicWithServerSideEncryption(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [NotSimulatorFact]
    public async Task Can_Create_Topic_With_Encryption()
    {
        // Arrange
        ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();
        IAwsClientFactory clientFactory = CreateClientFactory();

        var client = clientFactory.GetSnsClient(Region);

        var topic = new SnsTopicByName(
            UniqueName,
            client,
            loggerFactory);

        // Act
        await topic.CreateWithEncryptionAsync(new ServerSideEncryption { KmsMasterKeyId = JustSayingConstants.DefaultSnsAttributeEncryptionKeyId }, CancellationToken.None);

        // Assert
        topic.ServerSideEncryption.KmsMasterKeyId.ShouldBe(JustSayingConstants.DefaultSnsAttributeEncryptionKeyId);
    }

    [NotSimulatorFact]
    public async Task Can_Add_Encryption_To_Existing_Topic()
    {
        // Arrange
        ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();
        IAwsClientFactory clientFactory = CreateClientFactory();

        var client = clientFactory.GetSnsClient(Region);

        var topic = new SnsTopicByName(
            UniqueName,
            client,
            loggerFactory);

        await topic.CreateAsync(CancellationToken.None);

        // Act
        await topic.CreateWithEncryptionAsync(new ServerSideEncryption { KmsMasterKeyId = JustSayingConstants.DefaultSnsAttributeEncryptionKeyId }, CancellationToken.None);

        // Assert
        topic.ServerSideEncryption.KmsMasterKeyId.ShouldBe(JustSayingConstants.DefaultSnsAttributeEncryptionKeyId);
    }

    [NotSimulatorFact]
    public async Task Can_Update_Encryption_For_Existing_Topic()
    {
        // Arrange
        ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();
        IAwsClientFactory clientFactory = CreateClientFactory();

        var client = clientFactory.GetSnsClient(Region);

        var topic = new SnsTopicByName(
            UniqueName,
            client,
            loggerFactory);

        await topic.CreateWithEncryptionAsync(new ServerSideEncryption { KmsMasterKeyId = "previousKeyId" }, CancellationToken.None);

        // Act
        await topic.CreateWithEncryptionAsync(new ServerSideEncryption { KmsMasterKeyId = JustSayingConstants.DefaultSnsAttributeEncryptionKeyId }, CancellationToken.None);

        // Assert
        topic.ServerSideEncryption.KmsMasterKeyId.ShouldBe(JustSayingConstants.DefaultSnsAttributeEncryptionKeyId);
    }
}
