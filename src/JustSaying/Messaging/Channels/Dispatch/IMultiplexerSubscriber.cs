using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Channels.Interrogation;

namespace JustSaying.Messaging.Channels.Dispatch
{
    internal interface IMultiplexerSubscriber
    {
        Task Run(CancellationToken stoppingToken);
        void Subscribe(IAsyncEnumerable<IQueueMessageContext> messageSource);
    }
}
