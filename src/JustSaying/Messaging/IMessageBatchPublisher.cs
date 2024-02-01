using JustSaying.Messaging.Interrogation;
using JustSaying.Models;

namespace JustSaying.Messaging;

/// <summary>
/// Defines a publisher for batches of messages.
/// </summary>
public interface IMessageBatchPublisher : IInterrogable, IStartable
{
    /// <summary>
    /// Publishes a batch of messages.
    /// </summary>
    /// <param name="publisher">The publisher to use.</param>
    /// <param name="messages">The message(s) to publish.</param>
    /// <param name="metadata">The message batch metadata.</param>
    /// <param name="cancellationToken">The optional cancellation token to use.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to publish the messages.
    /// </returns>
    Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken = default);
}
