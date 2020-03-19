using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Context;

namespace JustSaying.Messaging.Channels.Multiplexer
{
    /// <summary>
    /// Reduces multiple input streams of messages into a single stream, interleaving them.
    /// </summary>
    internal interface IMultiplexer
    {
        Task Run(CancellationToken stoppingToken);
        void ReadFrom(params ChannelReader<IQueueMessageContext>[] readers);
        IAsyncEnumerable<IQueueMessageContext> GetMessagesAsync();
    }
}
