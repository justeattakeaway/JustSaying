using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Interrogation;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public interface ISubscriptionGroupCollection
    {
        Task Run(CancellationToken stoppingToken);
        SubscriptionGroupsInterrogationResult Interrogate();
    }
}
