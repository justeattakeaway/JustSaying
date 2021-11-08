using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class WhenHandlerIsDeclaredAsExactlyOnce : IntegrationTestBase
{
    public WhenHandlerIsDeclaredAsExactlyOnce(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
    }

    [AwsFact]
    public async Task Then_The_Handler_Only_Receives_The_Message_Once()
    {
        // Arrange
        var messageLock = new MessageLockStore();
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .AddSingleton<IMessageLockAsync>(messageLock)
            .ConfigureJustSaying((builder) =>
                builder.WithLoopbackTopic<SimpleMessage>(UniqueName,
                    c => c.WithMiddlewareConfiguration(m =>
                        m.UseExactlyOnce<SimpleMessage>("lock-simple-message")
                            .UseDefaults<SimpleMessage>(handler.GetType()))))
            .AddJustSayingHandlers(new[] { handler });

        var message = new SimpleMessage();
        string json = "";
        await WhenAsync(
            services,
            async (publisher, listener, serviceProvider, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(message, cancellationToken);
                await publisher.PublishAsync(message, cancellationToken);

                dynamic middlewares = ((dynamic)listener.Interrogate().Data).Middleware;
                json = JsonConvert.SerializeObject(middlewares, Formatting.Indented)
                    .Replace(UniqueName, "TestQueueName");

                await Patiently.AssertThatAsync(() =>
                {
                    handler.ReceivedMessages.Where(m => m.Id.ToString() == message.UniqueKey())
                        .ShouldHaveSingleItem();
                });
            });

        json.ShouldMatchApproved(c => c.SubFolder("Approvals"));
    }
}