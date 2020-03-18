using System;
using System.Collections.Generic;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Middleware;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels
{
    public class ConsumerConfig : IConsumerConfig
    {
        public ConsumerConfig()
        {
            DefaultConsumerCount = Environment.ProcessorCount * MessageConstants.ParallelHandlerExecutionPerCore;
            ConcurrencyGroupConfiguration = new ConcurrencyGroupConfiguration(DefaultConsumerCount);
        }

        public int BufferSize => 10;

        public int DefaultConsumerCount { get; }
        public ConcurrencyGroupConfiguration ConcurrencyGroupConfiguration { get; }

        public int MultiplexerCapacity => 100;

        public MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>> SqsMiddleware { get; private set; }
            = new DelegateMiddleware<GetMessagesContext, IList<Amazon.SQS.Model.Message>>();

        public MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>> WithSqsPolicy(
            Func<MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>>,
                MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>>> creator)
        {
            SqsMiddleware = MiddlewareBuilder.WithAsync(SqsMiddleware, creator);
            return SqsMiddleware;
        }

        public MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>> WithDefaultSqsPolicy(
            ILoggerFactory loggerFactory)
        {
            SqsMiddleware = MiddlewareBuilder.WithAsync(SqsMiddleware,
                _ => new DefaultSqsMiddleware(loggerFactory.CreateLogger<DefaultSqsMiddleware>()));
            return SqsMiddleware;
        }
    }
}
