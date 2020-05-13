using System;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware;
using ReceiveMiddleware =
    JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Channels.Context.GetMessagesContext,
        System.Collections.Generic.IList<Amazon.SQS.Model.Message>>;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    /// <summary>
    /// Configures the default settings for all subscription groups.
    /// </summary>
    public class SubscriptionConfigBuilder
    {
        public SubscriptionConfigBuilder()
        {
            DefaultBufferSize = MessageConstants.MaxAmazonMessageCap;
            DefaultReceiveBufferReadTimeout = TimeSpan.FromMinutes(5);
            DefaultMultiplexerCapacity = 100;
            DefaultPrefetch = 10;
            DefaultConcurrencyLimit = Environment.ProcessorCount * MessageConstants.ParallelHandlerExecutionPerCore;
        }

        public int DefaultPrefetch { get; private set; }
        public int DefaultBufferSize { get; private set; }
        public TimeSpan DefaultReceiveBufferReadTimeout { get; private set; }
        public int DefaultConcurrencyLimit { get; private set; }
        public int DefaultMultiplexerCapacity { get; private set; }
        public ReceiveMiddleware SqsMiddleware { get; private set; }

        /// <summary>
        /// Specifies the default maximum amount of time to wait for messages to be available on each SQS queue in a
        /// <see cref="ISubscriptionGroup"/> before resetting the connection.
        /// Defaults to 5 minutes
        /// </summary>
        /// <param name="receiveBufferReadTimeout">The maximum amount of time to wait to read from each SQS queue</param>
        /// <returns>This builder object.</returns>
        public SubscriptionConfigBuilder WithDefaultReceiveBufferReadTimeout(TimeSpan receiveBufferReadTimeout)
        {
            DefaultReceiveBufferReadTimeout = receiveBufferReadTimeout;
            return this;
        }

        /// <summary>
        /// Specifies the default number of messages that may be buffered across all of the queues in this <see cref="ISubscriptionGroup"/>.
        /// Note: This setting is shared across all queues in this group. For per-queue settings, see <see cref="WithDefaultBufferSize"/>
        /// Defaults to 100
        /// </summary>
        /// <param name="multiplexerCapacity">The maximum multiplexer capacity</param>
        /// <returns>This builder object.</returns>
        public SubscriptionConfigBuilder WithDefaultMultiplexerCapacity(int multiplexerCapacity)
        {
            DefaultMultiplexerCapacity = multiplexerCapacity;
            return this;
        }

        /// <summary>
        /// Specifies the default number of messages to try and fetch from SQS per attempt for each queue in a <see cref="ISubscriptionGroup"/>
        /// Defaults to 10
        /// </summary>
        /// <param name="prefetch">the number of messages to load per request</param>
        /// <returns>This builder object.</returns>
        public SubscriptionConfigBuilder WithDefaultPrefetch(int prefetch)
        {
            DefaultPrefetch = prefetch;
            return this;
        }

        /// <summary>
        /// Specifies the default maximum number of messages that may be processed at once by a <see cref="ISubscriptionGroup"/>.
        /// Defaults to Environment.ProcessorCount * 4
        /// </summary>
        /// <param name="concurrencyLimit">The maximum number of messages to process at the same time</param>
        /// <returns>This builder object.</returns>
        public SubscriptionConfigBuilder WithDefaultConcurrencyLimit(int concurrencyLimit)
        {
            DefaultConcurrencyLimit = concurrencyLimit;
            return this;
        }

        /// <summary>
        /// Specifies the default number of messages that will be buffered from SQS for each of the queues in a <see cref="ISubscriptionGroup"/>
        /// before waiting for them to drain into the <see cref="IMultiplexer"/>.
        /// Note: This setting is per-queue. To set the shared buffer size for all queues, see <see cref="WithDefaultMultiplexerCapacity"/>
        /// </summary>
        /// <param name="bufferSize">The maximum number of messages for each queue to buffer</param>
        /// <returns>This builder object.</returns>
        public SubscriptionConfigBuilder WithDefaultBufferSize(int bufferSize)
        {
            DefaultBufferSize = bufferSize;
            return this;
        }

        /// <summary>
        /// Overrides the default middleware used by the receive pipeline, which performs some default error handling
        /// (see <see cref="DefaultSqsMiddleware"/>)
        /// </summary>
        /// <param name="middleware">A <see cref="ReceiveMiddleware"/> that replaces the default middleware
        /// (see <see cref="DefaultSqsMiddleware"/>)</param>
        /// <returns>The builder object</returns>
        public SubscriptionConfigBuilder WithCustomMiddleware(ReceiveMiddleware middleware)
        {
            SqsMiddleware = middleware;
            return this;
        }

        public void Validate()
        {
            if (DefaultPrefetch < 0)
                throw new InvalidOperationException($"{nameof(DefaultPrefetch)} cannot be negative");
            
            if (DefaultPrefetch > MessageConstants.MaxAmazonMessageCap)
                throw new InvalidOperationException(
                    $"{nameof(DefaultPrefetch)} cannot be greater than {nameof(MessageConstants.MaxAmazonMessageCap)}");

            if (DefaultReceiveBufferReadTimeout < TimeSpan.Zero)
                throw new InvalidOperationException($"{nameof(DefaultReceiveBufferReadTimeout)} cannot be negative");

            if (DefaultConcurrencyLimit < 0)
                throw new InvalidOperationException($"{nameof(DefaultConcurrencyLimit)} cannot be negative");

            if (DefaultMultiplexerCapacity < 0)
                throw new InvalidOperationException($"{nameof(DefaultMultiplexerCapacity)} cannot be negative");

            if (DefaultBufferSize < 0)
                throw new InvalidOperationException($"{nameof(DefaultBufferSize)} cannot be negative");
        }
    }
}
