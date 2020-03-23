using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Channels.ConsumerGroups
{
    public interface IConsumerGroup
    {
        Task Run(CancellationToken stoppingToken);
    }
}
