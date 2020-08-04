using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Interrogation;

namespace JustSaying
{
    /// <summary>
    /// Defines a messaging bus.
    /// </summary>
    public interface IMessagingBus : IInterrogable
    {
        /// <summary>
        /// Starts the message bus as an asynchronous operation.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which will stop the bus when signalled.</param>
        /// <returns>A <see cref="Task"/> that completes when the bus is canceled.</returns>
        Task StartAsync(CancellationToken stoppingToken);
    }
}
