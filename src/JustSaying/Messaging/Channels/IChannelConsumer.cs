using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Channels
{
    internal interface IChannelConsumer
    {
        Task Run(CancellationToken stoppingToken);
        IChannelConsumer ConsumeFrom(IAsyncEnumerable<IQueueMessageContext> messageSource);
    }
}
