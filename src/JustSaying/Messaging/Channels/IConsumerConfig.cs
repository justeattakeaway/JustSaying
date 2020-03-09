using System;
using System.Collections.Generic;
using JustSaying.Messaging.Policies;

namespace JustSaying.Messaging.Channels
{
    public interface IConsumerConfig
    {
        int BufferSize { get; }

        int MultiplexerCapacity { get; }

        int ConsumerCount { get; }

        SqsPolicyAsync<IList<Amazon.SQS.Model.Message>> SqsPolicy { get; }
    }
}
