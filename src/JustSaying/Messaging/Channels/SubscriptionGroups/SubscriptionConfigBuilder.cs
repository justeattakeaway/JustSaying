using System;
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
            DefaultReceiveBufferWriteTimeout = TimeSpan.FromSeconds(2);
            DefaultMultiplexerCapacity = 100;
            DefaultPrefetch = 10;
            DefaultConcurrencyLimit = Environment.ProcessorCount * MessageConstants.ParallelHandlerExecutionPerCore;
        }

        public int DefaultPrefetch { get; private set; }
        public int DefaultBufferSize { get; private set; }
        public TimeSpan DefaultReceiveBufferReadTimeout { get; private set; }
        public TimeSpan DefaultReceiveBufferWriteTimeout { get; private set; }
        public int DefaultConcurrencyLimit { get; private set; }
        public int DefaultMultiplexerCapacity { get; private set; }
        public ReceiveMiddleware SqsMiddleware { get; private set; }

        /// <summary>
        /// Sets the default duration to wait to write messages to the multiplexer between checking for cancellation
        /// </summary>
        /// <param name="receiveBufferWriteTimeout"></param>
        /// <returns></returns>
        public SubscriptionConfigBuilder WithDefaultReceiveBufferWriteTimeout(TimeSpan receiveBufferWriteTimeout)
        {
            DefaultReceiveBufferWriteTimeout = receiveBufferWriteTimeout;
            return this;
        }

        /// <summary>
        /// Sets the default duration to wait to read from SQS before starting a new long polling connection
        /// </summary>
        /// <param name="receiveBufferReadTimeout"></param>
        /// <returns></returns>
        public SubscriptionConfigBuilder WithDefaultReceiveBufferReadTimeout(TimeSpan receiveBufferReadTimeout)
        {
            DefaultReceiveBufferReadTimeout = receiveBufferReadTimeout;
            return this;
        }

        /// <summary>
        /// Sets the default capacity of the multiplexer
        /// </summary>
        /// <param name="multiplexerCapacity"></param>
        /// <returns></returns>
        public SubscriptionConfigBuilder WithDefaultMultiplexerCapacity(int multiplexerCapacity)
        {
            DefaultMultiplexerCapacity = multiplexerCapacity;
            return this;
        }

        /// <summary>
        /// Sets the default number of messages to attempt to fetch in each request to SQS.
        /// </summary>
        /// <param name="prefetch">The number of messages to load. Must be between 1 and 10</param>
        /// <returns></returns>
        public SubscriptionConfigBuilder WithDefaultPrefetch(int prefetch)
        {
            DefaultPrefetch = prefetch;
            return this;
        }

        /// <summary>
        /// Sets the default maximum number of messages that may be processed at a time, per subscription group.
        /// </summary>
        /// <param name="concurrencyLimit"></param>
        /// <returns></returns>
        public SubscriptionConfigBuilder WithDefaultConcurrencyLimit(int concurrencyLimit)
        {
            DefaultConcurrencyLimit = concurrencyLimit;
            return this;
        }

        /// <summary>
        /// Sets the default number of messages to buffer in the MessageReceiveBuffer before they are sent to the Multiplexer
        /// </summary>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
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
    }
}
