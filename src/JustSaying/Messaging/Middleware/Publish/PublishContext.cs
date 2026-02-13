using JustSaying.Models;

namespace JustSaying.Messaging.Middleware;

/// <summary>
/// Context for publish middleware, containing the message(s) and metadata being published.
/// </summary>
public sealed class PublishContext
{
    /// <summary>
    /// Creates a context for a single message publish.
    /// </summary>
    public PublishContext(Message message, PublishMetadata metadata)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Metadata = metadata ?? new PublishMetadata();
    }

    /// <summary>
    /// Creates a context for a batch message publish.
    /// </summary>
    public PublishContext(IReadOnlyCollection<Message> messages, PublishMetadata metadata)
    {
        Messages = messages ?? throw new ArgumentNullException(nameof(messages));
        Metadata = metadata ?? new PublishBatchMetadata();
    }

    /// <summary>
    /// Gets the message being published, or null for batch publishes.
    /// </summary>
    public Message Message { get; }

    /// <summary>
    /// Gets the messages being published in a batch, or null for single publishes.
    /// </summary>
    public IReadOnlyCollection<Message> Messages { get; }

    /// <summary>
    /// Gets the publish metadata. Middleware can add message attributes to this.
    /// </summary>
    public PublishMetadata Metadata { get; }
}
