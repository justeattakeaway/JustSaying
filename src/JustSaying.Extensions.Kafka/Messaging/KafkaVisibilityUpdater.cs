using JustSaying.Messaging.Channels.Context;

namespace JustSaying.Extensions.Kafka.Messaging;

/// <summary>
/// A no-op visibility updater for Kafka since Kafka doesn't use visibility timeouts.
/// </summary>
internal class KafkaVisibilityUpdater : IMessageVisibilityUpdater
{
    public Task UpdateMessageVisibilityTimeout(TimeSpan visibilityTimeout, CancellationToken cancellationToken = default)
    {
        // Kafka doesn't use SQS-style visibility timeouts
        return Task.CompletedTask;
    }
}
