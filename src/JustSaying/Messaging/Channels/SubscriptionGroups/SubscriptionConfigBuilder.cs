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

        public SubscriptionConfigBuilder WithBufferSize(int bufferSize)
        {
            DefaultBufferSize = bufferSize;
            return this;
        }

        public SubscriptionConfigBuilder WithSqsPolicy(Func<ReceiveMiddleware, ReceiveMiddleware> creator)
        {
            SqsMiddleware = SqsMiddleware.WithAsync(creator);
            return this;
        }

        public SubscriptionConfigBuilder WithDefaultSqsPolicy(ILoggerFactory loggerFactory)
        {
            SqsMiddleware = SqsMiddleware.WithAsync(_ =>
                new DefaultSqsMiddleware(loggerFactory.CreateLogger<DefaultSqsMiddleware>()));
            return this;
        }
    }
}
