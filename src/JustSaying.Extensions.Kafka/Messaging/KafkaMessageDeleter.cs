using JustSaying.Messaging.Channels.Context;

namespace JustSaying.Extensions.Kafka.Messaging;

/// <summary>
/// A no-op message deleter for Kafka since Kafka uses commit-based acknowledgment.
/// </summary>
internal class KafkaMessageDeleter : IMessageDeleter
{
    public Task DeleteMessage(CancellationToken cancellationToken = default)
    {
        // Kafka doesn't use SQS-style deletion - messages are committed instead
        return Task.CompletedTask;
    }
}
