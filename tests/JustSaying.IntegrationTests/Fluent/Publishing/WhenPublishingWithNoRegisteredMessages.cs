using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenPublishingWithNoRegisteredMessages : IntegrationTestBase
{
    [Test]
    public async Task Then_An_Exception_Is_Thrown()
    {
        // Arrange
        var serviceProvider = GivenJustSaying()
            .BuildServiceProvider();

        var publisher = serviceProvider.GetService<IMessagePublisher>();
        await publisher.StartAsync(CancellationToken.None);

        // Act and Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => publisher.PublishAsync(new SimpleMessage()));
        exception.Message.ShouldBe("Error publishing message, no publishers registered. Has the bus been started?");


        var batchPublisher = serviceProvider.GetService<IMessageBatchPublisher>();
        await batchPublisher.StartAsync(CancellationToken.None);

        // Act and Assert
        exception = await Should.ThrowAsync<InvalidOperationException>(() => batchPublisher.PublishAsync([new SimpleMessage()], CancellationToken.None));
        exception.Message.ShouldBe("Error publishing message batch, no publishers registered. Has the bus been started?");
    }
}
