using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging;

public interface IMessagePublisher<in TMessage> : IInterrogable where TMessage : class
{
    Task PublishAsync(TMessage message, CancellationToken cancellationToken);
    Task PublishAsync(TMessage message, PublishMetadata metadata, CancellationToken cancellationToken);
}

public interface IMessagePublisher : IInterrogable, IStartable
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : class;
    Task PublishAsync<TMessage>(TMessage message, PublishMetadata metadata, CancellationToken cancellationToken) where TMessage : class;
}
