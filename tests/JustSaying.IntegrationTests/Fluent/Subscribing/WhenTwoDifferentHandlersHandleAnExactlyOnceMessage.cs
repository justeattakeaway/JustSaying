using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware.ExactlyOnce;
using JustSaying.Messaging.Middleware.Handle;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenTwoDifferentHandlersHandleAnExactlyOnceMessage : IntegrationTestBase
    {
        public WhenTwoDifferentHandlersHandleAnExactlyOnceMessage(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

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
                        t => t.WithReadConfiguration(rc =>
                            rc.WithMiddlewareConfiguration(m =>
                                m.UseExactlyOnce<SimpleMessage>("some-key")))))
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
                    await Task.Delay(1.Seconds(), cancellationToken);

                    // Assert
                    handler1.ReceivedMessages.ShouldHaveSingleItem().Id.ShouldBe(message.Id);
                    handler2.ReceivedMessages.ShouldHaveSingleItem().Id.ShouldBe(message.Id);
                });
        }
    }
}
