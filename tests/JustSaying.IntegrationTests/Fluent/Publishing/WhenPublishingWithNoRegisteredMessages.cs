using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenPublishingWithNoRegisteredMessages(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_An_Exception_Is_Thrown()
    {
        // Arrange
        var serviceProvider = GivenJustSaying()
            .BuildServiceProvider();

        var publisher = serviceProvider.GetService<IMessagePublisher>();
        await publisher.StartAsync(CancellationToken.None);

        // Act and Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(new SimpleMessage()));
        exception.Message.ShouldBe("Error publishing message, no publishers registered. Has the bus been started?");


        var batchPublisher = serviceProvider.GetService<IMessageBatchPublisher>();
        await batchPublisher.StartAsync(CancellationToken.None);

        // Act and Assert
        exception = await Assert.ThrowsAsync<InvalidOperationException>(() => batchPublisher.PublishAsync([new SimpleMessage()], CancellationToken.None));
        exception.Message.ShouldBe("Error publishing message batch, no publishers registered. Has the bus been started?");
    }
}
