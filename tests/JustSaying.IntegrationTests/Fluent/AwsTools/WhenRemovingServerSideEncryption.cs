using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using Microsoft.Extensions.Logging;

#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.AwsTools;

public class WhenRemovingServerSideEncryption : IntegrationTestBase
{
    public WhenRemovingServerSideEncryption(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
    }

    [NotSimulatorFact]
    public async Task Can_Remove_Encryption()
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
            new SqsBasicConfiguration { ServerSideEncryption = new ServerSideEncryption() });

        // Act
        await queue.UpdateQueueAttributeAsync(
            new SqsBasicConfiguration { ServerSideEncryption = null }, CancellationToken.None);

        // Assert
        queue.ServerSideEncryption.ShouldBeNull();
    }
}