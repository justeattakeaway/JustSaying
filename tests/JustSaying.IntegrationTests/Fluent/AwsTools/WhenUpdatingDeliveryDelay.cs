using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;

#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.AwsTools;

public class WhenUpdatingDeliveryDelay(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Can_Update_Delivery_Delay()
    {
        // Arrange
        var oldDeliveryDelay = TimeSpan.FromMinutes(2);
        var newDeliveryDelay = TimeSpan.FromMinutes(5);

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
            new SqsBasicConfiguration() { DeliveryDelay = oldDeliveryDelay });

        // Act
        await queue.UpdateQueueAttributeAsync(
            new SqsBasicConfiguration() { DeliveryDelay = newDeliveryDelay }, CancellationToken.None);

        // Assert
        queue.DeliveryDelay.ShouldBe(newDeliveryDelay);
    }
}