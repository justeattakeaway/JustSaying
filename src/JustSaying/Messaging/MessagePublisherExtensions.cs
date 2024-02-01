using System.ComponentModel;
using JustSaying.Models;

namespace JustSaying.Messaging;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MessagePublisherExtensions
{
    public static Task PublishAsync(this IMessagePublisher publisher, Message message)
    {
        return publisher.PublishAsync(message, CancellationToken.None);
    }

    public static async Task PublishAsync(this IMessagePublisher publisher,
        Message message, PublishMetadata metadata)
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        await publisher.PublishAsync(message, metadata, CancellationToken.None)
            .ConfigureAwait(false);
    }

    public static async Task PublishAsync(this IMessagePublisher publisher,
        Message message, CancellationToken cancellationToken)
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        await publisher.PublishAsync(message, null, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes a batch of messages.
    /// </summary>
    /// <param name="publisher">The publisher to use.</param>
    /// <param name="messages">The message(s) to publish.</param>
    /// <param name="cancellationToken">The optional cancellation token to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to publish the messages.
    /// </returns>
    public static Task PublishAsync(this IMessageBatchPublisher publisher, IEnumerable<Message> messages, CancellationToken cancellationToken = default)
        => publisher.PublishAsync(messages, null, cancellationToken);

    /// <summary>
    /// Publishes a collection of messages.
    /// </summary>
    /// <param name="publisher">The publisher to use.</param>
    /// <param name="messages">The message(s) to publish.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to publish the messages.
    /// </returns>
    public static Task PublishAsync(this IMessagePublisher publisher, IEnumerable<Message> messages)
        => publisher.PublishAsync(messages, null, CancellationToken.None);

    /// <summary>
    /// Publishes a collection of messages.
    /// </summary>
    /// <param name="publisher">The publisher to use.</param>
    /// <param name="messages">The message(s) to publish.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to publish the messages.
    /// </returns>
    public static Task PublishAsync(this IMessagePublisher publisher, IEnumerable<Message> messages, CancellationToken cancellationToken)
        => publisher.PublishAsync(messages, null, cancellationToken);

    /// <summary>
    /// Publishes a collection of messages.
    /// </summary>
    /// <param name="publisher">The publisher to use.</param>
    /// <param name="messages">The message(s) to publish.</param>
    /// <param name="metadata">The message batch metadata.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to publish the messages.
    /// </returns>
    public static Task PublishAsync(this IMessagePublisher publisher, IEnumerable<Message> messages, PublishBatchMetadata metadata)
        => publisher.PublishAsync(messages, metadata, CancellationToken.None);

    /// <summary>
    /// Publishes a collection of messages.
    /// </summary>
    /// <param name="publisher">The publisher to use.</param>
    /// <param name="messages">The message(s) to publish.</param>
    /// <param name="metadata">The message batch metadata.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to publish the messages.
    /// </returns>
    public static Task PublishAsync(
        this IMessagePublisher publisher,
        IEnumerable<Message> messages,
        PublishBatchMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        if (publisher is IMessageBatchPublisher batchPublisher)
        {
            return batchPublisher.PublishAsync(messages, metadata, cancellationToken);
        }

        return PublishAllMessagesAsync(publisher, messages, metadata, cancellationToken);

        static async Task PublishAllMessagesAsync(IMessagePublisher publisher, IEnumerable<Message> messages, PublishMetadata metadata, CancellationToken cancellationToken)
        {
            foreach (var message in messages)
            {
                await publisher.PublishAsync(message, metadata, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
