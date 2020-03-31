using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Interrogation;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public interface ISubscriptionGroupCollection : IInterrogable
    {
        Task Run(CancellationToken stoppingToken);
    }
}
