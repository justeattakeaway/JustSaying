using System.ComponentModel;

namespace JustSaying.Messaging;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MessagePublisherExtensions
{
    public static Task PublishAsync<TMessage>(this IMessagePublisher publisher, TMessage message) where TMessage : class
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        return publisher.PublishAsync(message, CancellationToken.None);
    }

    public static Task PublishAsync<TMessage>(this IMessagePublisher publisher, TMessage message, PublishMetadata metadata) where TMessage : class
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        return publisher.PublishAsync(message, metadata, CancellationToken.None);
    }

    /// <summary>
    /// Publishes a batch of messages.
    /// </summary>
    /// <param name="publisher">The batch publisher to use.</param>
    /// <param name="messages">The message(s) to publish.</param>
    /// <param name="cancellationToken">The optional cancellation token to use.</param>
    /// <typeparam name="TMessage">The type of the messages to publish.</typeparam>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to publish the messages.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> is <see langword="null"/>.</exception>
    public static Task PublishBatchAsync<TMessage>(this IMessageBatchPublisher publisher, IEnumerable<TMessage> messages, CancellationToken cancellationToken) where TMessage : class
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        return publisher.PublishBatchAsync(messages, null, cancellationToken);
    }

    /// <summary>
    /// Publishes a collection of messages, using batch publishing if supported by the publisher,
    /// otherwise publishing each message individually.
    /// </summary>
    /// <param name="publisher">The publisher to use.</param>
    /// <param name="messages">The message(s) to publish.</param>
    /// <typeparam name="TMessage">The type of the messages to publish.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> is <see langword="null"/>.</exception>
    public static Task PublishBatchAsync<TMessage>(this IMessagePublisher publisher, IEnumerable<TMessage> messages) where TMessage : class
        => publisher.PublishBatchAsync(messages, null, CancellationToken.None);

    /// <summary>
    /// Publishes a collection of messages, using batch publishing if supported by the publisher,
    /// otherwise publishing each message individually.
    /// </summary>
    /// <param name="publisher">The publisher to use.</param>
    /// <param name="messages">The message(s) to publish.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <typeparam name="TMessage">The type of the messages to publish.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> is <see langword="null"/>.</exception>
    public static Task PublishBatchAsync<TMessage>(this IMessagePublisher publisher, IEnumerable<TMessage> messages, CancellationToken cancellationToken) where TMessage : class
        => publisher.PublishBatchAsync(messages, null, cancellationToken);

    /// <summary>
    /// Publishes a collection of messages, using batch publishing if supported by the publisher,
    /// otherwise publishing each message individually.
    /// </summary>
    /// <param name="publisher">The publisher to use.</param>
    /// <param name="messages">The message(s) to publish.</param>
    /// <param name="metadata">The message batch metadata.</param>
    /// <typeparam name="TMessage">The type of the messages to publish.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> is <see langword="null"/>.</exception>
    public static Task PublishBatchAsync<TMessage>(this IMessagePublisher publisher, IEnumerable<TMessage> messages, PublishBatchMetadata metadata) where TMessage : class
        => publisher.PublishBatchAsync(messages, metadata, CancellationToken.None);

    /// <summary>
    /// Publishes a collection of messages, using batch publishing if supported by the publisher,
    /// otherwise publishing each message individually.
    /// </summary>
    /// <param name="publisher">The publisher to use.</param>
    /// <param name="messages">The message(s) to publish.</param>
    /// <param name="metadata">The message batch metadata.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <typeparam name="TMessage">The type of the messages to publish.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> is <see langword="null"/>.</exception>
    public static Task PublishBatchAsync<TMessage>(
        this IMessagePublisher publisher,
        IEnumerable<TMessage> messages,
        PublishBatchMetadata metadata,
        CancellationToken cancellationToken) where TMessage : class
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        if (publisher is IMessageBatchPublisher batchPublisher)
        {
            return batchPublisher.PublishBatchAsync(messages, metadata, cancellationToken);
        }

        return PublishAllMessagesAsync(publisher, messages, metadata, cancellationToken);

        static async Task PublishAllMessagesAsync(IMessagePublisher publisher, IEnumerable<TMessage> messages, PublishMetadata metadata, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
            {
                await publisher.PublishAsync(message, metadata, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
