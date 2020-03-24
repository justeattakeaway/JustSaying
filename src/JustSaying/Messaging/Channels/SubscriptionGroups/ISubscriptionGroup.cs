using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public interface ISubscriptionGroup
    {
        Task Run(CancellationToken stoppingToken);
    }
}

