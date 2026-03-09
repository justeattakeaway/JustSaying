using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.DependencyInjection.Microsoft;

public class WhenRegisteringAHandlerViaContainerWithMissingRegistration : IntegrationTestBase
{
    [Test]
    public void Then_An_Exception_Is_Thrown()
    {
        // Arrange
        var future = new Future<OrderPlaced>();

        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackQueue<OrderPlaced>(UniqueName))
            .BuildServiceProvider();

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(() => serviceProvider.GetService<IMessagingBus>());
        exception.Message.ShouldBe("No handler for message type JustSaying.IntegrationTests.TestHandlers.OrderPlaced is registered.");
    }
}