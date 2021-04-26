using System.Linq;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
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
                        c =>
                            c.WithReadConfiguration(rc =>
                                rc.WithMiddlewareConfiguration(m =>
                                    m.UseExactlyOnce<SimpleMessage>("simple-message-lock")))))
                .AddJustSayingHandlers(new[] { handler });

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    await listener.StartAsync(cancellationToken);
                    await publisher.StartAsync(cancellationToken);

                    var message = new SimpleMessage();

                    // Act
                    await publisher.PublishAsync(message, cancellationToken);
                    await publisher.PublishAsync(message, cancellationToken);
                    await Task.Delay(1.Seconds(), cancellationToken);

                    handler.ReceivedMessages.Where(m => m.Id.ToString() == message.UniqueKey())
                        .ShouldHaveSingleItem();
                });
        }
    }
}
