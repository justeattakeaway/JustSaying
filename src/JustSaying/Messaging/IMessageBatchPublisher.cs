using JustSaying.Models;

namespace JustSaying.Messaging;

public interface IMessageBatchPublisher
{
    Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken);
    Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken);
}

