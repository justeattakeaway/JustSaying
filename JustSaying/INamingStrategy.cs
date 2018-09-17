using System;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying
{
    public interface INamingStrategy
    {
        string GetTopicName(string baseTopicName, Type messageType);
        string GetQueueName(SqsReadConfiguration sqsConfig, Type messageType);
    }
}
