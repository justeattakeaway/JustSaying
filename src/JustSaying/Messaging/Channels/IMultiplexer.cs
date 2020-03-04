using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.Channels
{
    internal interface IMultiplexer
    {
        Task Run(CancellationToken stoppingToken);
        void ReadFrom(ChannelReader<IQueueMessageContext> reader);
        IAsyncEnumerable<IQueueMessageContext> Messages();
        Task Completion { get; }
    }
}
