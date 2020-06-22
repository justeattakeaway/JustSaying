using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    /// <summary>
    /// Coordinates reading messages from a collection of <see cref="IMessageReceiveBuffer"/>
    /// and dispatching using a collection of <see cref="IMultiplexerSubscriber"/>.
    /// </summary>
    public interface ISubscriptionGroup : IInterrogable
    {
        /// <summary>
        /// RunAsync
        /// </summary>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> that will stop all running tasks.</param>
        /// <returns>A <see cref="Task"/> that will complete once all the running tasks have completed.</returns>
        Task RunAsync(CancellationToken stoppingToken);
    }
}
