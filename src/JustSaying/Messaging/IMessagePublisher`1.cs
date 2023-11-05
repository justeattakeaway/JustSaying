using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging;

public interface IMessagePublisher<in TMessage> : IInterrogable where TMessage : class
{
    Task PublishAsync(TMessage message, CancellationToken cancellationToken);
    Task PublishAsync(TMessage message, PublishMetadata metadata, CancellationToken cancellationToken);
}
