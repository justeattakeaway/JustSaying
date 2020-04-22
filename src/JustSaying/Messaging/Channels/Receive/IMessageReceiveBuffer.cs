using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging.Channels.Receive
{
    internal interface IMessageReceiveBuffer : IInterrogable
    {
        Task Run(CancellationToken stoppingToken);
        ChannelReader<IQueueMessageContext> Reader { get; }
    }
}
