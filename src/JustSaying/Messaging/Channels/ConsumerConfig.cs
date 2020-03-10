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
            ConsumerCount = Environment.ProcessorCount * MessageConstants.ParallelHandlerExecutionPerCore;
        }

        public int BufferSize => 10;

        public int ConsumerCount { get; }

        public int MultiplexerCapacity => 100;

        public MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>> SqsMiddleware { get; private set; }
                    = new NoopMiddleware<GetMessagesContext, IList<Amazon.SQS.Model.Message>>();

        public MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>> WithSqsPolicy(Func<MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>>, MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>>> creator)
        {
            SqsMiddleware = MiddlewareBuilder.WithAsync(SqsMiddleware, creator);
            return SqsMiddleware;
        }

        public MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>> WithDefaultSqsPolicy(ILoggerFactory loggerFactory)
        {
            SqsMiddleware = MiddlewareBuilder.WithAsync(SqsMiddleware, _ => new DefaultSqsMiddleware(loggerFactory.CreateLogger<DefaultSqsMiddleware>()));
            return SqsMiddleware;
        }
    }
}
