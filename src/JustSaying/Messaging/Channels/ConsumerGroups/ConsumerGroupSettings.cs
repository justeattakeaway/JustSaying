using System.Collections.Generic;
using System.Linq;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels.ConsumerGroups
{
    public class ConsumerGroupSettings
    {
        private readonly List<ISqsQueue> _sqsQueues;

        public ConsumerGroupSettings(
            int bufferSize,
            int consumerCount,
            int multiplexerCapcity,
            int prefetch,
            IList<ISqsQueue> sqsQueues = null)
        {
            BufferSize = bufferSize;
            ConsumerCount = consumerCount;
            MultiplexerCapacity = multiplexerCapcity;
            Prefetch = prefetch;
            _sqsQueues = sqsQueues?.ToList() ?? new List<ISqsQueue>();
        }

        public void AddQueue(ISqsQueue sqsQueue)
        {
            _sqsQueues.Add(sqsQueue);
        }

        public int ConsumerCount { get; }
        public int BufferSize { get; }
        public int MultiplexerCapacity { get; }
        public int Prefetch { get; }

        public IReadOnlyList<ISqsQueue> Queues => _sqsQueues;
    }
}
