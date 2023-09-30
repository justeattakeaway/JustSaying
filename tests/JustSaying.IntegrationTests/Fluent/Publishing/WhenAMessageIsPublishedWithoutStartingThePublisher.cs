using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessageIsPublishedWithoutStartingThePublisher(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_PushlishShouldThrow()
    {
        // Arrange
        var completionSource = new TaskCompletionSource<object>();
        var handler = CreateHandler<SimpleMessage>(completionSource);

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackQueue<SimpleMessage>(UniqueName))
            .AddSingleton(handler);

        var message = new SimpleMessage()
        {
            Content = Guid.NewGuid().ToString()
        };

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => publisher.PublishAsync(message, cancellationToken));
            });
    }
}
