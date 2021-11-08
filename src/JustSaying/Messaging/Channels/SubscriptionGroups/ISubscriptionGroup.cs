using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.Dispatch;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    /// <summary>
    /// Coordinates reading messages from a collection of <see cref="IMessageReceiveBuffer"/>
    /// and dispatching using a collection of <see cref="IMultiplexerSubscriber"/>.
    /// </summary>
    public interface ISubscriptionGroup : IInterrogable
    {
        /// <summary>
        /// Runs the buffer, multiplexer and subscriber and will complete once all the tasks have completed.
        /// </summary>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> that will stop all running tasks.</param>
        /// <returns>A <see cref="Task"/> that will complete once all the running tasks have completed.</returns>
        Task RunAsync(CancellationToken stoppingToken);
    }
}
