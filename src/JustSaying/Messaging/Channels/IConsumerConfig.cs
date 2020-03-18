using System;
using System.Collections.Generic;
using JustSaying.Messaging.Middleware;

namespace JustSaying.Messaging.Channels
{
    public interface IConsumerConfig
    {
        int BufferSize { get; }

        int MultiplexerCapacity { get; }

        int DefaultConsumerCount { get; }
        ConcurrencyGroupConfiguration ConcurrencyGroupConfiguration { get; }

        MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>> SqsMiddleware { get; }
    }
}
