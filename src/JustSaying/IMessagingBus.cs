using System.Threading;
using System.Threading.Tasks;

namespace JustSaying
{
    /// <summary>
    /// Defines a messaging bus.
    /// </summary>
    public interface IMessagingBus
    {
        /// <summary>
        /// Starts the message bus as an asynchronous operation.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which will stop the bus when signalled.</param>
        Task Start(CancellationToken cancellationToken);
    }
}
