using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels;

namespace JustSaying.AwsTools.MessageHandling.Dispatch
{
    public interface IMessageDispatcher
    {
        Task DispatchMessageAsync(IQueueMessageContext messageContext, CancellationToken cancellationToken);
    }
}
