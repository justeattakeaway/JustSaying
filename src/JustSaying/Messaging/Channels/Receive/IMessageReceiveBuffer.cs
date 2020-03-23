using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Context;

namespace JustSaying.Messaging.Channels.Receive
{
    internal interface IMessageReceiveBuffer
    {
        Task Run(CancellationToken stoppingToken);
        ChannelReader<IQueueMessageContext> Reader { get; }
    }
}
