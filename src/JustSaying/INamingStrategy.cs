using System;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying
{
    public interface INamingStrategy
    {
        string GetTopicName(string topicName, Type messageType);
        string GetQueueName(SqsReadConfiguration sqsConfig, Type messageType);
    }
}
