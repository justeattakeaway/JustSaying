using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging;

public interface IMessagePublisher : IInterrogable, IStartable
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : class;
    Task PublishAsync<TMessage>(TMessage message, PublishMetadata metadata, CancellationToken cancellationToken) where TMessage : class;
}
