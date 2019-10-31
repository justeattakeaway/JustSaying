using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface IMessageCoordinator
    {
        Task ListenAsync(CancellationToken cancellationToken);
        void WithMessageProcessingStrategy(IMessageProcessingStrategy messageProcessingStrategy);
        string QueueName { get; }
        string Region { get; }
    }
}
