using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;

#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.AwsTools;

public class WhenANamedQueueIsCreated(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Error_Queue_Is_Created()
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

        // Act
        await queue.CreateAsync(new SqsBasicConfiguration());

        // Assert
        await Patiently.AssertThatAsync(OutputHelper,
            async () => await queue.ErrorQueue.ExistsAsync(CancellationToken.None),
            40.Seconds());
    }
}
