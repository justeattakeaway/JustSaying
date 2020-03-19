using System;
using System.Collections.Generic;
using JustSaying.Messaging.Channels.ConsumerGroups;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Configuration
{
    public class ConsumerGroupConfig
    {
        public ConsumerGroupConfig()
        {
            DefaultBufferSize = 10;
            DefaultMultiplexerCapacity = 100;
            DefaultPrefetch = 10;
            DefaultConsumerCount = Environment.ProcessorCount * MessageConstants.ParallelHandlerExecutionPerCore;

            ConsumerGroupConfiguration =
                new ConsumerGroupConfiguration(
                    DefaultConsumerCount,
                    DefaultBufferSize,
                    DefaultMultiplexerCapacity,
                    DefaultPrefetch);
        }

        public int DefaultPrefetch { get; set; }
        public int DefaultBufferSize { get; set; }
        public int DefaultConsumerCount { get; set; }
        public int DefaultMultiplexerCapacity { get; set; }
        public ConsumerGroupConfiguration ConsumerGroupConfiguration { get; }

        public MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>> SqsMiddleware { get; private set; }
            = new DelegateMiddleware<GetMessagesContext, IList<Amazon.SQS.Model.Message>>();

        public MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>> WithSqsPolicy(
            Func<MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>>,
                MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>>> creator)
        {
            SqsMiddleware = SqsMiddleware.WithAsync(creator);
            return SqsMiddleware;
        }

        public MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>> WithDefaultSqsPolicy(
            ILoggerFactory loggerFactory)
        {
            SqsMiddleware = SqsMiddleware.WithAsync(_ =>
                new DefaultSqsMiddleware(loggerFactory.CreateLogger<DefaultSqsMiddleware>()));
            return SqsMiddleware;
        }
    }
}
