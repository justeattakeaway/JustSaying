using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using Microsoft.Extensions.Logging;

#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.AwsTools;

public class WhenRemovingSnsServerSideEncryption(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [NotSimulatorFact]
    public async Task Can_Remove_Encryption()
    {
        // Arrange
        ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();
        IAwsClientFactory clientFactory = CreateClientFactory();

        var client = clientFactory.GetSnsClient(Region);

        var topic = new SnsTopicByName(
            UniqueName,
            client,
            loggerFactory);

        await topic.CreateWithEncryptionAsync(new ServerSideEncryption { KmsMasterKeyId = JustSayingConstants.DefaultSnsAttributeEncryptionKeyId }, CancellationToken.None);

        // Act
        await topic.CreateWithEncryptionAsync(new ServerSideEncryption { KmsMasterKeyId = string.Empty }, CancellationToken.None);

        // Assert
        topic.ServerSideEncryption.ShouldBeNull();
    }
}
