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

    public static Task PublishAsync(this IMessagePublisher publisher, IEnumerable<Message> messages)
        => publisher.PublishAsync(messages, null, CancellationToken.None);

    public static Task PublishAsync(this IMessagePublisher publisher, IEnumerable<Message> messages, CancellationToken cancellationToken)
        => publisher.PublishAsync(messages, null, cancellationToken);

    public static Task PublishAsync(this IMessagePublisher publisher, IEnumerable<Message> messages, PublishBatchMetadata metadata)
        => publisher.PublishAsync(messages, metadata, CancellationToken.None);

    public static Task PublishAsync(this IMessagePublisher publisher, IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken)
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
