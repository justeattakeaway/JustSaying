using JustSaying.Extensions;
using JustSaying.Models;

namespace JustSaying.Messaging;

internal static class MessageBatchPublisherExtensions
{
    // Both SNS and SQS accept at most ten messages in a batch.
    private const int MaximumBatchSize = 10;

    /// <summary>
    /// Prepares the requests needed to publish a batch of messages, without sending anything.
    /// </summary>
    /// <param name="publisher">The publisher to use.</param>
    /// <param name="messages">The message(s) to publish.</param>
    /// <param name="metadata">The optional message batch metadata.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <returns>
    /// The requests to make, each of which can be sent, and sent again, independently of the others.
    /// </returns>
    public static async Task<IReadOnlyList<PreparedBatch>> PrepareBatchesAsync(
        this IMessageBatchPublisher publisher,
        IReadOnlyCollection<Message> messages,
        PublishBatchMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (publisher is IPreparedBatchPublisher preparedPublisher)
        {
            return await preparedPublisher.PrepareAsync(messages, metadata, cancellationToken).ConfigureAwait(false);
        }

        // A publisher that cannot say what requests it will make is handed the messages a chunk at a time,
        // which is as close to a single request as can be assumed.
        int batchSize = Math.Min(metadata?.BatchSize ?? MaximumBatchSize, MaximumBatchSize);

        return
        [
            .. messages
                .Chunk(batchSize)
                .Select(chunk => new PreparedBatch(chunk, token => publisher.PublishAsync(chunk, metadata, token)))
        ];
    }
}
