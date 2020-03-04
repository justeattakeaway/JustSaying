using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Channels
{
    internal interface IMessageReceiveBuffer
    {
        Task Run(CancellationToken stoppingToken);
        ChannelReader<IQueueMessageContext> Reader { get; }
    }
}
