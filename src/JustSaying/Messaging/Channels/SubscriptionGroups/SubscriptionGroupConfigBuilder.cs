using System;
using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public class SubscriptionGroupConfigBuilder
    {
        private readonly List<ISqsQueue> _sqsQueues;

        private int? _bufferSize;
        private TimeSpan? _receiveBufferReadTimeout;
        private TimeSpan? _receiveBufferWriteTimeout;
        private int? _concurrencyLimit;
        private int? _multiplexerCapacity;
        private int? _prefetch;

        private readonly string _groupName;

        public SubscriptionGroupConfigBuilder(string groupName)
        {
            _groupName = groupName;
            _sqsQueues = new List<ISqsQueue>();
        }

        public SubscriptionGroupConfigBuilder AddQueue(ISqsQueue sqsQueue)
        {
            _sqsQueues.Add(sqsQueue);
            return this;
        }

        public SubscriptionGroupConfigBuilder AddQueues(IEnumerable<ISqsQueue> sqsQueues)
        {
            _sqsQueues.AddRange(sqsQueues);
            return this;
        }

        public SubscriptionGroupConfigBuilder WithConcurrencyLimit(int concurrencyLimit)
        {
            _concurrencyLimit = concurrencyLimit;
            return this;
        }

        public SubscriptionGroupConfigBuilder WithBufferSize(int bufferSize)
        {
            _bufferSize = bufferSize;
            return this;
        }

        public SubscriptionGroupConfigBuilder WithReceiveBufferReadTimeout(TimeSpan receiveBufferReadTimeout)
        {
            _receiveBufferReadTimeout = receiveBufferReadTimeout;
            return this;
        }

        public SubscriptionGroupConfigBuilder WithReceiveBufferWriteTimeout(TimeSpan receiveBufferWriteTimeout)
        {
            _receiveBufferWriteTimeout = receiveBufferWriteTimeout;
            return this;
        }

        public SubscriptionGroupConfigBuilder WithMultiplexerCapacity(int multiplexerCapacity)
        {
            _multiplexerCapacity = multiplexerCapacity;
            return this;
        }

        public SubscriptionGroupConfigBuilder WithPrefetch(int prefetch)
        {
            _prefetch = prefetch;
            return this;
        }

        public SubscriptionGroupSettings Build(SubscriptionConfigBuilder defaults)
        {
            if (defaults == null) throw new InvalidOperationException("Defaults must be set before building settings");

            return new SubscriptionGroupSettings(
                _groupName,
                _concurrencyLimit ?? defaults.DefaultConcurrencyLimit,
                _bufferSize ?? defaults.DefaultBufferSize,
                _receiveBufferReadTimeout ?? defaults.DefaultReceiveBufferReadTimeout,
                _receiveBufferWriteTimeout ?? defaults.DefaultReceiveBufferWriteTimeout,
                _multiplexerCapacity ?? defaults.DefaultMultiplexerCapacity,
                _prefetch ?? defaults.DefaultPrefetch,
                _sqsQueues);
        }
    }
}
