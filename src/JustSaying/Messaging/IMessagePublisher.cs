using JustSaying.Messaging.Interrogation;
using JustSaying.Models;

namespace JustSaying.Messaging;

public interface IMessagePublisher : IInterrogable, IStartable
{
    Task PublishAsync(Message message, CancellationToken cancellationToken);
    Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken);
}
