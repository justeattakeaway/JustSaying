using System.ComponentModel;

namespace JustSaying.Messaging;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MessagePublisherExtensions
{
    public static Task PublishAsync<T>(this IMessagePublisher<T> publisher, T message) where T : class
    {
        return publisher.PublishAsync<T>(message, CancellationToken.None);
    }

    public static async Task PublishAsync<TMessage>(this IMessagePublisher<TMessage> publisher,
        TMessage message, PublishMetadata metadata) where TMessage : class
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        await publisher.PublishAsync(message, metadata, CancellationToken.None)
            .ConfigureAwait(false);
    }

    public static async Task PublishAsync<TMessage>(this IMessagePublisher<TMessage> publisher,
        TMessage message, CancellationToken cancellationToken) where TMessage : class
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        await publisher.PublishAsync(message, null, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task PublishAsync<T>(this IMessagePublisher publisher,
        T message) where T : class
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        await publisher.PublishAsync(message, CancellationToken.None)
            .ConfigureAwait(false);
    }

    public static async Task PublishAsync<TMessage>(this IMessagePublisher publisher,
        TMessage message, PublishMetadata metadata) where TMessage : class
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        await publisher.PublishAsync(message, metadata, CancellationToken.None)
            .ConfigureAwait(false);
    }

    public static async Task PublishAsync<TMessage>(this IMessagePublisher publisher,
        TMessage message, CancellationToken cancellationToken) where TMessage : class
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        await publisher.PublishAsync(message, null, cancellationToken)
            .ConfigureAwait(false);
    }
}
