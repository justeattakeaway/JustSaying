using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenRegisteringATopicForAGenericMessage : IntegrationTestBase
    {
        public WhenRegisteringATopicForAGenericMessage(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_The_Message_Is_Handled()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<object>();
            var handler = CreateHandler<GenericMessage<MyMessage>>(completionSource);

            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) => builder.WithLoopbackTopic<GenericMessage<MyMessage>>(UniqueName))
                .AddSingleton(handler);

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    await listener.StartAsync(cancellationToken);
                    await publisher.StartAsync(cancellationToken);

                    // Act
                    await publisher.PublishAsync(new GenericMessage<MyMessage>(), cancellationToken);

                    // Assert
                    completionSource.Task.Wait(cancellationToken);
                });
        }
    }
}
