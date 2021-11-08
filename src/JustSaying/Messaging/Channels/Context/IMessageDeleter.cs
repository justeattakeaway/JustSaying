namespace JustSaying.Messaging.Channels.Context;

/// <summary>
/// Provides a way to delete a message once it has been successfully handled.
/// </summary>
public interface IMessageDeleter
{
    /// <summary>
    /// Will delete this message from the queue.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>A Task that will be completed when the message has been deleted or the operation fails.</returns>
    Task DeleteMessage(CancellationToken cancellationToken);
}