using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class WhenTwoDifferentHandlersHandleAnExactlyOnceMessage(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_Both_Handlers_Receive_The_Message()
    {
        var messageLock = new MessageLockStore();

        var handler1 = new InspectableHandler<SimpleMessage>();
        var handler2 = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .AddSingleton<IMessageLockAsync>(messageLock)
            .ConfigureJustSaying((builder) =>
                builder.WithLoopbackTopic<SimpleMessage>(UniqueName,
                    t => t.WithMiddlewareConfiguration(m =>
                    {
                        m.UseExactlyOnce<SimpleMessage>("some-key");
                        m.UseDefaults<SimpleMessage>(handler1.GetType());
                    })))
            .AddJustSayingHandlers(new[] { handler1, handler2 });

        await WhenAsync(
            services,
            async (publisher, listener, serviceProvider, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                var message = new SimpleMessage();

                // Act
                await publisher.PublishAsync(message, cancellationToken);

                // Assert
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        handler1.ReceivedMessages.ShouldHaveSingleItem().Id.ShouldBe(message.Id);
                        handler2.ReceivedMessages.ShouldHaveSingleItem().Id.ShouldBe(message.Id);
                    });
            });
    }
}