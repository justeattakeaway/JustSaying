using System;
using System.Collections.Generic;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    /// <summary>
    /// Configures overrides for a particular <see cref="ISubscriptionGroup"/>. At build time, defaults provided by
    /// <see cref="SubscriptionGroupSettingsBuilder"/> are combined with overrides set here to create a final configuration
    /// that is inspectable via <see cref="IInterrogable"/>.
    /// </summary>
    public class SubscriptionGroupConfigBuilder
    {
        private readonly List<ISqsQueue> _sqsQueues;

        private int? _bufferSize;
        private TimeSpan? _receiveBufferReadTimeout;
        private TimeSpan? _recieveMessagesWaitTime;
        private int? _concurrencyLimit;
        private int? _multiplexerCapacity;
        private int? _prefetch;

        private readonly string _groupName;

        /// <summary>
        /// Creates an instance of <see cref="SubscriptionGroupConfigBuilder"/>.
        /// </summary>
        /// <param name="groupName">The name of the subscription group.</param>
        public SubscriptionGroupConfigBuilder(string groupName)
        {
            _groupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
            _sqsQueues = new List<ISqsQueue>();
        }

        /// <summary>
        /// Adds an <see cref="ISqsQueue"/> to be consumed by this <see cref="ISubscriptionGroup"/>.
        /// </summary>
        /// <param name="sqsQueue">The queue to be consumed, assumed to already be created and ready.</param>
        /// <returns>This builder object.</returns>
        public SubscriptionGroupConfigBuilder AddQueue(ISqsQueue sqsQueue)
        {
            _sqsQueues.Add(sqsQueue);
            return this;
        }

        /// <summary>
        /// Adds a collection of <see cref="ISqsQueue"/> to be consumed by this <see cref="ISubscriptionGroup"/>.
        /// </summary>
        /// <param name="sqsQueues">The queues to be consumed, assumed to already be created and ready.</param>
        /// <returns>This builder object.</returns>
        public SubscriptionGroupConfigBuilder AddQueues(IEnumerable<ISqsQueue> sqsQueues)
        {
            _sqsQueues.AddRange(sqsQueues);
            return this;
        }

        /// <summary>
        /// Specifies the maximum number of messages that may be processed at once by this <see cref="ISubscriptionGroup"/>.
        /// </summary>
        /// <param name="concurrencyLimit">The maximum number of messages to process at the same time.</param>
        /// <returns>This builder object.</returns>
        public SubscriptionGroupConfigBuilder WithConcurrencyLimit(int concurrencyLimit)
        {
            _concurrencyLimit = concurrencyLimit;
            return this;
        }

        /// <summary>
        /// Specifies the number of messages that will be buffered from SQS for each of the queues in this <see cref="ISubscriptionGroup"/>
        /// before waiting for them to drain into the <see cref="IMultiplexer"/>.
        /// Note: This setting is per-queue. To set the shared buffer size for all queues, see <see cref="WithMultiplexerCapacity"/>.
        /// </summary>
        /// <param name="bufferSize">The maximum number of messages for each queue to buffer.</param>
        /// <returns>This builder object.</returns>
        public SubscriptionGroupConfigBuilder WithBufferSize(int bufferSize)
        {
            _bufferSize = bufferSize;
            return this;
        }

        /// <summary>
        /// Specifies the maximum amount of time to wait for messages to be available on each SQS queue in this
        /// <see cref="ISubscriptionGroup"/> before resetting the connection.
        /// </summary>
        /// <param name="receiveBufferReadTimeout">The maximum amount of time to wait to read from each SQS queue.</param>
        /// <returns>This builder object.</returns>
        public SubscriptionGroupConfigBuilder WithReceiveBufferReadTimeout(TimeSpan receiveBufferReadTimeout)
        {
            _receiveBufferReadTimeout = receiveBufferReadTimeout;
            return this;
        }

        /// <summary>
        /// Specifies the default duration SQS will wait for a message before returning if there are no messages.
        /// </summary>
        /// <param name="waitTime">The maximum amount of time SQS should wait before returning.</param>
        /// <returns>This builder object.</returns>
        public SubscriptionGroupConfigBuilder WithReceiveMessagesWaitTime(TimeSpan waitTime)
        {
            _recieveMessagesWaitTime = waitTime;
            return this;
        }

        /// <summary>
        /// Specifies the number of messages that may be buffered across all of the queues in this <see cref="ISubscriptionGroup"/>.
        /// Note: This setting is shared across all queues in this group. For per-queue settings, see <see cref="WithBufferSize"/>.
        /// </summary>
        /// <param name="multiplexerCapacity">The maximum multiplexer capacity.</param>
        /// <returns>This builder object.</returns>
        public SubscriptionGroupConfigBuilder WithMultiplexerCapacity(int multiplexerCapacity)
        {
            _multiplexerCapacity = multiplexerCapacity;
            return this;
        }

        /// <summary>
        /// Specifies the number of messages to try and fetch from the SQS per attempt for each queue in this <see cref="ISubscriptionGroup"/>.
        /// </summary>
        /// <param name="prefetch">the number of messages to load per request.</param>
        /// <returns>This builder object.</returns>
        public SubscriptionGroupConfigBuilder WithPrefetch(int prefetch)
        {
            _prefetch = prefetch;
            return this;
        }

        /// <summary>
        /// Given a set of defaults and overrides from this builder, builds a concrete <see cref="SubscriptionGroupSettings"/>
        /// that can be passed to an <see cref="ISubscriptionGroupFactory"/> to build an <see cref="ISubscriptionGroup"/>.
        /// </summary>
        /// <param name="defaults">The default values to use if no override given.</param>
        /// <returns>A <see cref="SubscriptionGroupSettings"/>.</returns>
        public SubscriptionGroupSettings Build(SubscriptionGroupSettingsBuilder defaults)
        {
            if (defaults == null) throw new InvalidOperationException("Defaults must be set before building settings.");

            var settings = new SubscriptionGroupSettings(
                _groupName,
                _concurrencyLimit ?? defaults.ConcurrencyLimit,
                _bufferSize ?? defaults.BufferSize,
                _receiveBufferReadTimeout ?? defaults.ReceiveBufferReadTimeout,
                _recieveMessagesWaitTime ?? defaults.ReceiveMessagesWaitTime,
                _multiplexerCapacity ?? defaults.MultiplexerCapacity,
                _prefetch ?? defaults.Prefetch,
                _sqsQueues);

            settings.Validate();

            return settings;
        }
    }
}
