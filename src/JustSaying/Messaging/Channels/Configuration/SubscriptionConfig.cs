using System;
using System.Collections.Generic;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware;
using Microsoft.Extensions.Logging;

using ReceiveMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Channels.Configuration.GetMessagesContext, System.Collections.Generic.IList<Amazon.SQS.Model.Message>>;

namespace JustSaying.Messaging.Channels.Configuration
{
    public class SubscriptionConfig
    {
        internal SubscriptionConfig()
        {
            DefaultBufferSize = MessageConstants.MaxAmazonMessageCap;
            DefaultMultiplexerCapacity = 100;
            DefaultPrefetch = 10;
            DefaultConcurrencyLimit = Environment.ProcessorCount * MessageConstants.ParallelHandlerExecutionPerCore;
        }

        public int DefaultPrefetch { get; set; }
        public int DefaultBufferSize { get; set; }
        public int DefaultConcurrencyLimit { get; set; }
        public int DefaultMultiplexerCapacity { get; set; }

        public ReceiveMiddleware SqsMiddleware { get; private set; }
            = new DelegateMiddleware<GetMessagesContext, IList<Amazon.SQS.Model.Message>>();

        public ReceiveMiddleware WithSqsPolicy(Func<ReceiveMiddleware, ReceiveMiddleware> creator)
        {
            SqsMiddleware = SqsMiddleware.WithAsync(creator);
            return SqsMiddleware;
        }

        public ReceiveMiddleware WithDefaultSqsPolicy(ILoggerFactory loggerFactory)
        {
            SqsMiddleware = SqsMiddleware.WithAsync(_ =>
                new DefaultSqsMiddleware(loggerFactory.CreateLogger<DefaultSqsMiddleware>()));
            return SqsMiddleware;
        }
    }
}
