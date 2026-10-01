using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

/// <summary>
/// Publishing routes by each message's runtime type, so a publication for an interface or abstract
/// type could never be used. It's rejected when the publisher is built rather than failing on every
/// publish. Subscriptions to such types are still allowed.
/// </summary>
public class WhenRegisteringAPublicationForANonConcreteType : IntegrationTestBase
{
    public interface IOrderEvent;

    public abstract class OrderEvent : IOrderEvent;

    [Test]
    public void Then_An_Interface_Publication_Throws()
    {
        AssertPublicationThrows(pub => pub.WithTopic<IOrderEvent>(), "an interface");
    }

    [Test]
    public void Then_An_Abstract_Publication_Throws()
    {
        AssertPublicationThrows(pub => pub.WithQueue<OrderEvent>(q => q.WithQueueName(UniqueName)), "abstract");
    }

    [Test]
    public void Then_An_Abstract_Subscription_Is_Still_Allowed()
    {
        // Arrange
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying(builder => builder.Subscriptions(sub => sub.ForQueue<OrderEvent>(q => q.WithQueueName(UniqueName))))
            .AddSingleton(Substitute.For<IHandlerAsync<OrderEvent>>())
            .BuildServiceProvider();

        // Act and Assert
        serviceProvider.GetRequiredService<IMessagingBus>().ShouldNotBeNull();
    }

    private void AssertPublicationThrows(Action<PublicationsBuilder> configure, string reason)
    {
        // Arrange
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying(builder => builder.Publications(configure))
            .BuildServiceProvider();

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(() => serviceProvider.GetRequiredService<IMessagePublisher>());
        exception.Message.ShouldContain(reason);
        exception.Message.ShouldContain("concrete");
    }
}
