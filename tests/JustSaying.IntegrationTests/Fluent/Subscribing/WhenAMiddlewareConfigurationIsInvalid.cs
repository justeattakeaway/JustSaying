using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class WhenAMiddlewareConfigurationIsInvalid : IntegrationTestBase
{
    [Test]
    public void Then_A_Configuration_That_Never_Adds_The_Handler_Throws()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();

        var serviceProvider = GivenJustSaying()
            .AddSingleton<IMessageLockAsync>(new MessageLockStore())
            .ConfigureJustSaying((builder) =>
                builder.WithLoopbackTopic<SimpleMessage>(UniqueName,
                    c => c.WithMiddlewareConfiguration(m =>
                        m.UseExactlyOnce<SimpleMessage>("lock-simple-message"))))
            .AddJustSayingHandlers(new[] { handler })
            .BuildServiceProvider();

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(() => serviceProvider.GetService<IMessagingBus>());
        exception.Message.ShouldContain(typeof(SimpleMessage).FullName);
        exception.Message.ShouldContain("UseDefaults<SimpleMessage>");
    }

    [Test]
    public void Then_Exactly_Once_For_A_Different_Message_Type_Throws()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();

        var serviceProvider = GivenJustSaying()
            .AddSingleton<IMessageLockAsync>(new MessageLockStore())
            .ConfigureJustSaying((builder) =>
                builder.WithLoopbackTopic<SimpleMessage>(UniqueName,
                    c => c.WithMiddlewareConfiguration(m =>
                        m.UseExactlyOnce<AnotherSimpleMessage>("lock-simple-message")
                            .UseDefaults<SimpleMessage>(handler.GetType()))))
            .AddJustSayingHandlers(new[] { handler })
            .BuildServiceProvider();

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(() => serviceProvider.GetService<IMessagingBus>());
        exception.Message.ShouldContain("UseExactlyOnce<AnotherSimpleMessage>");
        exception.Message.ShouldContain(typeof(SimpleMessage).FullName);
    }

    [Test]
    public void Then_A_Multi_Type_Registration_That_Never_Adds_The_Handler_Throws()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();

        var serviceProvider = GivenJustSaying()
            .AddSingleton<IMessageLockAsync>(new MessageLockStore())
            .ConfigureJustSaying((builder) =>
                builder.Subscriptions(s => s.ForQueue(UniqueName, q =>
                    q.Handling<SimpleMessage>(middlewareConfiguration: m =>
                        m.UseExactlyOnce<SimpleMessage>("lock-simple-message")))))
            .AddJustSayingHandlers(new[] { handler })
            .BuildServiceProvider();

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(() => serviceProvider.GetService<IMessagingBus>());
        exception.Message.ShouldContain(typeof(SimpleMessage).FullName);
        exception.Message.ShouldContain("UseDefaults<SimpleMessage>");
    }
}
