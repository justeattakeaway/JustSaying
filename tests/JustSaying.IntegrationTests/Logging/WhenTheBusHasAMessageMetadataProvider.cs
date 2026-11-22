using JustSaying.IntegrationTests.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace JustSaying.Logging;

/// <summary>
/// The bus has one <see cref="IMessageMetadataProvider"/>, and both the publish and handle logs read
/// message identity through it.
/// </summary>
public class WhenTheBusHasAMessageMetadataProvider : IntegrationTestBase
{
    private sealed class PrefixingMetadataProvider : IMessageMetadataProvider
    {
        public string GetId(object message) => message is Message typed ? $"custom-{typed.Id}" : null;

        public DateTimeOffset? GetTimestamp(object message) => null;

        public bool TryGetDeduplicationKey(object message, out string deduplicationKey)
        {
            deduplicationKey = null;
            return false;
        }
    }

    [Test]
    public async Task Then_Publish_And_Handle_Logs_Use_It()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying(levelOverride: LogLevel.Information)
            .AddSingleton(new MessagingConfig { MessageMetadataProvider = new PrefixingMetadataProvider() })
            .ConfigureJustSaying(builder => builder.WithLoopbackQueue<SimpleMessage>(UniqueName))
            .AddJustSayingHandlers([handler]);

        var message = new SimpleMessage();
        var expectedId = $"custom-{message.Id}";
        FakeLogCollector logs = null;

        await WhenAsync(
            services,
            async (publisher, listener, serviceProvider, cancellationToken) =>
            {
                logs = serviceProvider.GetFakeLogCollector();

                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(message, cancellationToken);

                await Patiently.AssertThatAsync(() =>
                    logs.GetSnapshot().ShouldContain(le => le.Message.StartsWith("Succeeded handling message", StringComparison.Ordinal)));
            });

        // Assert
        var snapshot = logs.GetSnapshot();
        snapshot.ShouldContain(le => le.Message.StartsWith($"Published message {expectedId} ", StringComparison.Ordinal));
        snapshot.ShouldContain(le => le.Message.StartsWith($"Succeeded handling message with Id '{expectedId}'", StringComparison.Ordinal));
    }
}
