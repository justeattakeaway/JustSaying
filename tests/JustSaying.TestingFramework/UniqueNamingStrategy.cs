using System;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;

namespace JustSaying.TestingFramework
{
    public sealed class UniqueNamingStrategy : INamingStrategy
    {
        private readonly long ticks = DateTime.UtcNow.Ticks;

        public string GetTopicName(string topicName, Type messageType)
        {
            return (messageType.ToTopicName() + ticks).ToLowerInvariant();
        }

        public string GetQueueName(SqsReadConfiguration sqsConfig, Type messageType)
        {
            return (sqsConfig.BaseQueueName + ticks).ToLowerInvariant();
        }
    }
}
