using JustSaying.Models;

namespace JustSaying.Messaging;

/// <summary>
/// Defines a batch publisher that can work out the requests it needs to make before making any of them.
/// </summary>
/// <remarks>
/// A batch can turn into several requests, as they are limited by combined size as well as by count. Preparing
/// them up front lets the caller retry a request that fails on its own, where retrying a call to
/// <see cref="IMessageBatchPublisher.PublishAsync"/> would publish again whatever that call had already sent.
/// </remarks>
internal interface IPreparedBatchPublisher
{
    /// <summary>
    /// Converts the messages and packs them into the requests needed to publish them, without sending anything.
    /// </summary>
    /// <param name="messages">The message(s) to publish.</param>
    /// <param name="metadata">The optional message batch metadata.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <returns>
    /// The requests to make, each of which can be sent, and sent again, independently of the others.
    /// </returns>
    Task<IReadOnlyList<PreparedBatch>> PrepareAsync(IReadOnlyCollection<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken);
}
