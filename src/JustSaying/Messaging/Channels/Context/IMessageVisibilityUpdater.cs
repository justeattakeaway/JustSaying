using System;
using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Channels.Context
{
    /// <summary>
    /// Provides a mechanism to update the visibility timeout for the message in the current context.
    /// </summary>
    public interface IMessageVisibilityUpdater
    {
        /// <summary>
        /// Will set the amount of time until this message will be visible again to consumers.
        /// </summary>
        /// <param name="visibilityTimeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateMessageVisibilityTimeout(TimeSpan visibilityTimeout, CancellationToken cancellationToken);
    }
}
