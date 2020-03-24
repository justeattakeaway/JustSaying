using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Configuration;

namespace JustSaying.Messaging.Channels.ConsumerGroups
{
    public class ConsumerGroupSettingsBuilder
    {
        private readonly List<ISqsQueue> _sqsQueues;

        public ConsumerGroupSettingsBuilder(ConsumerConfig defaultConfig)
        {
            _sqsQueues = new List<ISqsQueue>();
            _prefetch = defaultConfig.DefaultPrefetch;
            _bufferSize = defaultConfig.DefaultBufferSize;
            _consumerCount = defaultConfig.DefaultConsumerCount;
            _multiplexerCapacity = defaultConfig.DefaultMultiplexerCapacity;
        }

        public ConsumerGroupSettingsBuilder AddQueue(ISqsQueue sqsQueue)
        {
            _sqsQueues.Add(sqsQueue);
            return this;
        }

        public ConsumerGroupSettingsBuilder AddQueues(IEnumerable<ISqsQueue> sqsQueues)
        {
            _sqsQueues.AddRange(sqsQueues);
            return this;
        }

        private int _consumerCount;

        public ConsumerGroupSettingsBuilder WithConsumerCount(int consumerCount)
        {
            _consumerCount = consumerCount;
            return this;
        }

        private int _bufferSize;

        public ConsumerGroupSettingsBuilder WithBufferSize(int bufferSize)
        {
            _bufferSize = bufferSize;
            return this;
        }

        private int _multiplexerCapacity;

        public ConsumerGroupSettingsBuilder WithMultiplexerCapacity(int multiplexerCapacity)
        {
            _multiplexerCapacity = multiplexerCapacity;
            return this;
        }

        private int _prefetch;

        public ConsumerGroupSettingsBuilder WithPrefetch(int prefetch)
        {
            _prefetch = prefetch;
            return this;
        }

        internal ConsumerGroupSettings Build()
        {
            return new ConsumerGroupSettings(_consumerCount,
                _bufferSize,
                _multiplexerCapacity,
                _prefetch,
                _sqsQueues);
        }
    }
}
