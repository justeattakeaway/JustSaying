using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public interface ISubscriptionGroup : IInterrogable
    {
        Task RunAsync(CancellationToken stoppingToken);
    }
}
