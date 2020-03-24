using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Configuration;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public class SubscriptionGroupSettingsBuilder
    {
        private readonly List<ISqsQueue> _sqsQueues;

        internal SubscriptionGroupSettingsBuilder(SubscriptionConfig defaultConfig)
        {
            _sqsQueues = new List<ISqsQueue>();
            _prefetch = defaultConfig.DefaultPrefetch;
            _bufferSize = defaultConfig.DefaultBufferSize;
            _consumerCount = defaultConfig.DefaultConsumerCount;
            _multiplexerCapacity = defaultConfig.DefaultMultiplexerCapacity;
        }

        public SubscriptionGroupSettingsBuilder AddQueue(ISqsQueue sqsQueue)
        {
            _sqsQueues.Add(sqsQueue);
            return this;
        }

        public SubscriptionGroupSettingsBuilder AddQueues(IEnumerable<ISqsQueue> sqsQueues)
        {
            _sqsQueues.AddRange(sqsQueues);
            return this;
        }

        private int _consumerCount;

        public SubscriptionGroupSettingsBuilder WithConsumerCount(int consumerCount)
        {
            _consumerCount = consumerCount;
            return this;
        }

        private int _bufferSize;

        public SubscriptionGroupSettingsBuilder WithBufferSize(int bufferSize)
        {
            _bufferSize = bufferSize;
            return this;
        }

        private int _multiplexerCapacity;

        public SubscriptionGroupSettingsBuilder WithMultiplexerCapacity(int multiplexerCapacity)
        {
            _multiplexerCapacity = multiplexerCapacity;
            return this;
        }

        private int _prefetch;

        public SubscriptionGroupSettingsBuilder WithPrefetch(int prefetch)
        {
            _prefetch = prefetch;
            return this;
        }

        internal SubscriptionGroupSettings Build()
        {
            return new SubscriptionGroupSettings(_consumerCount,
                _bufferSize,
                _multiplexerCapacity,
                _prefetch,
                _sqsQueues);
        }
    }
}
