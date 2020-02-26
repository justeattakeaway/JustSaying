using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels;

namespace JustSaying.AwsTools.MessageHandling.Dispatch
{
    internal interface IMessageDispatcher
    {
        Task DispatchMessageAsync(IQueueMessageContext messageContext, CancellationToken cancellationToken);
    }
}
