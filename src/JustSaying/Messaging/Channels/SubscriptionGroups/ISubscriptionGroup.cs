using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Interrogation;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public interface ISubscriptionGroup
    {
        Task Run(CancellationToken stoppingToken);
        SubscriptionGroupInterrogationResult Interrogate();
    }
}
