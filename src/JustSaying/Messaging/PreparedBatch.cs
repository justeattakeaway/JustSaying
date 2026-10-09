using JustSaying.Models;

namespace JustSaying.Messaging;

/// <summary>
/// A single request to publish a batch of messages, which has been prepared but not yet sent.
/// </summary>
/// <param name="messages">The messages the request publishes.</param>
/// <param name="send">A delegate that sends the request. It may be called more than once.</param>
internal sealed class PreparedBatch(IReadOnlyCollection<Message> messages, Func<CancellationToken, Task> send)
{
    /// <summary>
    /// Gets the messages the request publishes.
    /// </summary>
    public IReadOnlyCollection<Message> Messages { get; } = messages;

    /// <summary>
    /// Sends the request.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    public Task SendAsync(CancellationToken cancellationToken) => send(cancellationToken);
}
