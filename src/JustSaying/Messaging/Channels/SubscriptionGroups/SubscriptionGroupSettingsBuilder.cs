using System;
using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public class SubscriptionGroupSettingsBuilder
    {
        private readonly List<ISqsQueue> _sqsQueues;

        private int? _bufferSize;
        private TimeSpan? _receiveBufferReadTimeout;
        private TimeSpan? _receiveBufferWriteTimeout;
        private int? _concurrencyLimit;
        private int? _multiplexerCapacity;
        private int? _prefetch;
        private SubscriptionConfig _defaults;

        private readonly string _groupName;

        internal SubscriptionGroupSettingsBuilder(string groupName)
        {
            _groupName = groupName;
            _sqsQueues = new List<ISqsQueue>();
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

        public SubscriptionGroupSettingsBuilder WithReceiveBufferReadTimeout(TimeSpan receiveBufferReadTimeout)
        {
            _receiveBufferReadTimeout = receiveBufferReadTimeout;
            return this;
        }

        public SubscriptionGroupSettingsBuilder WithReceiveBufferWriteTimeout(TimeSpan receiveBufferWriteTimeout)
        {
            _receiveBufferWriteTimeout = receiveBufferWriteTimeout;
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

        public SubscriptionGroupSettingsBuilder WithDefaultsFrom(SubscriptionConfig defaults)
        {
            _defaults = defaults;
            return this;
        }

        public SubscriptionGroupSettings Build()
        {
            if (_defaults == null) throw new InvalidOperationException("Defaults must be set before building settings");

            return new SubscriptionGroupSettings(
                _groupName,
                _concurrencyLimit ?? _defaults.DefaultConcurrencyLimit,
                _bufferSize ?? _defaults.DefaultBufferSize,
                _receiveBufferReadTimeout ?? _defaults.DefaultReceiveBufferReadTimeout,
                _receiveBufferWriteTimeout ?? _defaults.DefaultReceiveBufferWriteTimeout,
                _multiplexerCapacity ?? _defaults.DefaultMultiplexerCapacity,
                _prefetch ?? _defaults.DefaultPrefetch,
                _sqsQueues);
        }
    }
}
