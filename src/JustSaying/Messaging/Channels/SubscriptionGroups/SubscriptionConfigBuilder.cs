using System;
using System.Collections.Generic;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware;
using Microsoft.Extensions.Logging;
using ReceiveMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Channels.Context.GetMessagesContext, System.Collections.Generic.IList<Amazon.SQS.Model.Message>>;

namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
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
            = new DelegateMiddleware<GetMessagesContext, IList<Amazon.SQS.Model.Message>>();


        public SubscriptionConfigBuilder WithReceiveBufferWriteTimeout(TimeSpan receiveBufferWriteTimeout)
        {
            DefaultReceiveBufferWriteTimeout = receiveBufferWriteTimeout;
            return this;
        }

        public SubscriptionConfigBuilder WithReceiveBufferReadTimeout(TimeSpan receiveBufferReadTimeout)
        {
            DefaultReceiveBufferReadTimeout = receiveBufferReadTimeout;
            return this;
        }

        public SubscriptionConfigBuilder WithMultiplexerCapacity(int multiplexerCapacity)
        {
            DefaultMultiplexerCapacity = multiplexerCapacity;
            return this;
        }

        public SubscriptionConfigBuilder WithPrefetch(int prefetch)
        {
            DefaultPrefetch = prefetch;
            return this;
        }

        public SubscriptionConfigBuilder WithConcurrencyLimit(int concurrencyLimit)
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
        /// <param name="middleware">A provider func that takes a source ReceiveMiddleware and returns the custom ReceiveMiddleware</param>
        /// <returns>The builder object</returns>
        public SubscriptionConfigBuilder WithCustomMiddleware(ReceiveMiddleware middleware)
        {
            SqsMiddleware = middleware;
            return this;
        }
    }
}
