using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels.ConsumerGroups
{
    internal class ConsumerGroupSettings
    {
        internal ConsumerGroupSettings(
            int consumerCount,
            int bufferSize,
            int multiplexerCapacity,
            int prefetch,
            IReadOnlyList<ISqsQueue> queues)
        {
            ConsumerCount = consumerCount;
            BufferSize = bufferSize;
            MultiplexerCapacity = multiplexerCapacity;
            Prefetch = prefetch;
            Queues = queues;
        }

        public int ConsumerCount { get; }
        public int BufferSize { get; }
        public int MultiplexerCapacity { get; }
        public int Prefetch { get; }
        public IReadOnlyList<ISqsQueue> Queues { get; }
    }
}
