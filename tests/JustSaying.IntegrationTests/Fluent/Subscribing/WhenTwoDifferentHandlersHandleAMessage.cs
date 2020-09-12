using System.Threading.Tasks;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenTwoDifferentHandlersHandleAMessage : IntegrationTestBase
    {
        public WhenTwoDifferentHandlersHandleAMessage(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_Both_Handlers_Receive_The_Message()
        {
            // Arrange
            var handler1 = new InspectableHandler<SimpleMessage>();
            var handler2 = new InspectableHandler<SimpleMessage>();

            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) => builder.WithLoopbackTopic<SimpleMessage>(UniqueName))
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
                    handler1.ReceivedMessages.ShouldHaveSingleItem().UniqueKey().ShouldBe(message.UniqueKey());
                    handler2.ReceivedMessages.ShouldHaveSingleItem().UniqueKey().ShouldBe(message.UniqueKey());
                });
        }
    }
}
