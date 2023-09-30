using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;

#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.AwsTools;

public class WhenUpdatingRetentionPeriod(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Can_Update_Retention_Period()
    {
        // Arrange
        var oldRetentionPeriod = TimeSpan.FromSeconds(600);
        var newRetentionPeriod = TimeSpan.FromSeconds(700);

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
            new SqsBasicConfiguration { MessageRetention = oldRetentionPeriod });

        // Act
        await queue.UpdateQueueAttributeAsync(
            new SqsBasicConfiguration { MessageRetention = newRetentionPeriod }, CancellationToken.None);

        // Assert
        queue.MessageRetentionPeriod.ShouldBe(newRetentionPeriod);
    }
}