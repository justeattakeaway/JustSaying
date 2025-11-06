using System;
using System.Threading.Tasks;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Publishing
{
    public class WhenAMessageIsPublishedWithoutStartingThePublisher : IntegrationTestBase
    {
        public WhenAMessageIsPublishedWithoutStartingThePublisher(ITestOutputHelper outputHelper) : base(
            outputHelper)
        { }

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
}
