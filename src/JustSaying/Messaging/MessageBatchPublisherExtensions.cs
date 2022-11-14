using System.ComponentModel;
using JustSaying.Models;

namespace JustSaying.Messaging;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MessageBatchPublisherExtensions
{
    public static Task PublishAsync(this IMessageBatchPublisher publisher, IEnumerable<Message> messages)
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        return publisher.PublishAsync(messages, CancellationToken.None);
    }

    public static async Task PublishAsync(this IMessageBatchPublisher publisher, IEnumerable<Message> message, PublishBatchMetadata metadata)
    {
        if (publisher == null)
        {
            throw new ArgumentNullException(nameof(publisher));
        }

        await publisher.PublishAsync(message, metadata, CancellationToken.None)
            .ConfigureAwait(false);
    }
}
