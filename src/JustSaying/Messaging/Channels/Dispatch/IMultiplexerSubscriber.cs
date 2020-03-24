using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Context;

namespace JustSaying.Messaging.Channels.Dispatch
{
    internal interface IMultiplexerSubscriber
    {
        Task Run(CancellationToken stoppingToken);
        void ConsumeFrom(IAsyncEnumerable<IQueueMessageContext> messageSource);
    }
}
