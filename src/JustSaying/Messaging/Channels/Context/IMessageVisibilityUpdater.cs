namespace JustSaying.Messaging.Channels.Context;

/// <summary>
/// Provides a mechanism to update the visibility timeout for the message in the current context.
/// </summary>
public interface IMessageVisibilityUpdater
{
    /// <summary>
    /// Sets the amount of time until this message will be visible again to consumers.
    /// </summary>
    /// <param name="visibilityTimeout">The amount of time to wait until this message should become visible again.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> cancels the operation to update the visibility timeout.</param>
    /// <returns>A Task that will be completed when the messages visibility timeout has been updated or the operation fails.</returns>
    Task UpdateMessageVisibilityTimeout(TimeSpan visibilityTimeout, CancellationToken cancellationToken);
}