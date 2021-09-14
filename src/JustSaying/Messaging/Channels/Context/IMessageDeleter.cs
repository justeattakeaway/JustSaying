using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Channels.Context
{
    /// <summary>
    /// Provides a mechanism to delete messages once they've been successfully handled.
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
}
