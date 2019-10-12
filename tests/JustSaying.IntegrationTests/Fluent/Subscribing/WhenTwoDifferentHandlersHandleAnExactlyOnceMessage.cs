using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging;
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
            // Arrange
            var handler1 = new ExactlyOnceHandlerWithTimeout();
            var handler2 = new ExactlyOnceHandlerNoTimeout();

            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) => builder.WithLoopbackTopic<SimpleMessage>(UniqueName))
                .AddJustSayingHandlers(new[] { handler1, handler2 });

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    listener.Start(cancellationToken);

                    // Act
                    await publisher.PublishAsync(new SimpleMessage(), cancellationToken);
                    await Task.Delay(5.Seconds());

                    // Assert
                    handler1.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
                    handler2.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
                });
        }
    }
}
