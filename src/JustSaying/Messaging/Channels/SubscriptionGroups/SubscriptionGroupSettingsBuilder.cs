using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Configuration;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public class SubscriptionGroupSettingsBuilder
    {
        private readonly List<ISqsQueue> _sqsQueues;

        private int _bufferSize;
        private int _concurrencyLimit;
        private int _multiplexerCapacity;
        private int _prefetch;
        private readonly string _name;

        internal SubscriptionGroupSettingsBuilder(string name, SubscriptionConfig defaultConfig)
        {
            _name = name;
            _sqsQueues = new List<ISqsQueue>();
            _prefetch = defaultConfig.DefaultPrefetch;
            _bufferSize = defaultConfig.DefaultBufferSize;
            _concurrencyLimit = defaultConfig.DefaultConcurrencyLimit;
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

        public SubscriptionGroupSettingsBuilder WithConcurrencyLimit(int concurrencyLimit)
        {
            _concurrencyLimit = concurrencyLimit;
            return this;
        }

        public SubscriptionGroupSettingsBuilder WithBufferSize(int bufferSize)
        {
            _bufferSize = bufferSize;
            return this;
        }

        public SubscriptionGroupSettingsBuilder WithMultiplexerCapacity(int multiplexerCapacity)
        {
            _multiplexerCapacity = multiplexerCapacity;
            return this;
        }

        public SubscriptionGroupSettingsBuilder WithPrefetch(int prefetch)
        {
            _prefetch = prefetch;
            return this;
        }

        internal SubscriptionGroupSettings Build()
        {
            return new SubscriptionGroupSettings(
                _name,
                _concurrencyLimit,
                _bufferSize,
                _multiplexerCapacity,
                _prefetch,
                _sqsQueues);
        }
    }
}
