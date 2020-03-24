using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    internal class SubscriptionGroupSettings
    {
        internal SubscriptionGroupSettings(
            int concurrencyLimit,
            int bufferSize,
            int multiplexerCapacity,
            int prefetch,
            IReadOnlyCollection<ISqsQueue> queues)
        {
            ConcurrencyLimit = concurrencyLimit;
            BufferSize = bufferSize;
            MultiplexerCapacity = multiplexerCapacity;
            Prefetch = prefetch;
            Queues = queues;
        }

        public int ConcurrencyLimit { get; }
        public int BufferSize { get; }
        public int MultiplexerCapacity { get; }
        public int Prefetch { get; }
        public IReadOnlyCollection<ISqsQueue> Queues { get; }
    }
}
