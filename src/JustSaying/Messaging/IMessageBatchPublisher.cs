using JustSaying.Messaging.Interrogation;
using JustSaying.Models;

namespace JustSaying.Messaging;

public interface IMessageBatchPublisher : IInterrogable, IStartable
{
    Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken = default);
    Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken = default);
}

