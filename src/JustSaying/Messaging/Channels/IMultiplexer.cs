using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.Channels
{
    /// <summary>
    /// Reduces multiple input streams of messages into a single stream, interleaving them.
    /// </summary>
    internal interface IMultiplexer
    {
        Task Run(CancellationToken stoppingToken);
        void ReadFrom(ChannelReader<IQueueMessageContext> reader);
        IAsyncEnumerable<IQueueMessageContext> GetMessagesAsync();
    }
}
