using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging;

/// <summary>
/// Defines a publisher for batches of messages.
/// </summary>
public interface IMessageBatchPublisher : IInterrogable, IStartable
{
    /// <summary>
    /// Publishes a batch of messages.
    /// </summary>
    /// <param name="messages">The message(s) to publish.</param>
    /// <param name="metadata">The optional message batch metadata.</param>
    /// <param name="cancellationToken">The optional cancellation token to use.</param>
    /// <typeparam name="TMessage">The type of the messages to publish.</typeparam>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation to publish the messages.
    /// </returns>
    Task PublishBatchAsync<TMessage>(IEnumerable<TMessage> messages, PublishBatchMetadata metadata = default, CancellationToken cancellationToken = default) where TMessage : class;
}
