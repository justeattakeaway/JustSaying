using System;
using System.Collections.Generic;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Policies;

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

        public SqsPolicyAsync<IList<Amazon.SQS.Model.Message>> SqsPolicy { get; private set; }
                    = new NoopSqsPolicyAsync<IList<Amazon.SQS.Model.Message>>();

        public SqsPolicyAsync<IList<Amazon.SQS.Model.Message>> WithSqsPolicy(Func<SqsPolicyAsync<IList<Amazon.SQS.Model.Message>>, SqsPolicyAsync<IList<Amazon.SQS.Model.Message>>> policyCreator)
        {
            SqsPolicy = SqsPolicyBuilder.WithAsync(SqsPolicy, policyCreator);
            return SqsPolicy;
        }
    }
}
