using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;

#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.AwsTools;

public class WhenUpdatingRedrivePolicy(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Can_Update_Redrive_Policy()
    {
        // Arrange
        int maximumReceives = 42;

        ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();
        IAwsClientFactory clientFactory = CreateClientFactory();

        var client = clientFactory.GetSqsClient(Region);

        var queue = new SqsQueueByName(
            Region,
            UniqueName,
            client,
            1,
            loggerFactory);

        await queue.CreateAsync(new SqsBasicConfiguration());

        // Act
        await queue.UpdateRedrivePolicyAsync(
            new RedrivePolicy(maximumReceives, queue.ErrorQueue.Arn));

        // Assert
        queue.RedrivePolicy.ShouldNotBeNull();
        queue.RedrivePolicy.MaximumReceives.ShouldBe(maximumReceives);
    }
}