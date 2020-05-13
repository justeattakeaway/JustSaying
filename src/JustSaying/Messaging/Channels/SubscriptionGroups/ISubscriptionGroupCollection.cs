using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    /// <summary>
    /// Represents a collection of <see cref="ISubscriptionGroup"/> that can be started together.
    /// </summary>
    public interface ISubscriptionGroupCollection : IInterrogable
    {
        /// <summary>
        /// Starts all <see cref="ISubscriptionGroup"/> that are part of this collection
        /// </summary>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> that will cancel the subscription groups owned by this collection</param>
        /// <returns>A Task that completes when the bus is canceled.</returns>
        Task Run(CancellationToken stoppingToken);
    }
}
