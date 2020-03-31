using System;
using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    public class SubscriptionGroupSettingsBuilder
    {
        private readonly List<ISqsQueue> _sqsQueues;

        private int? _bufferSize;
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

        internal SubscriptionGroupSettingsBuilder AddQueue(ISqsQueue sqsQueue)
        {
            _sqsQueues.Add(sqsQueue);
            return this;
        }

        internal SubscriptionGroupSettingsBuilder AddQueues(IEnumerable<ISqsQueue> sqsQueues)
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

        public SubscriptionGroupSettingsBuilder WithDefaultsFrom(SubscriptionConfig defaults)
        {
            _defaults = defaults;
            return this;
        }

        internal SubscriptionGroupSettings Build()
        {
            if(_defaults == null) throw new InvalidOperationException("Defaults must be set before building settings");

            return new SubscriptionGroupSettings(
                _groupName,
                _concurrencyLimit ?? _defaults.DefaultConcurrencyLimit,
                _bufferSize ?? _defaults.DefaultBufferSize,
                _multiplexerCapacity ?? _defaults.DefaultMultiplexerCapacity,
                _prefetch ?? _defaults.DefaultPrefetch,
                _sqsQueues);
        }
    }
}
