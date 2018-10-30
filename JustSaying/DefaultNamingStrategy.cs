using System;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;

namespace JustSaying
{
    /// <summary>
    /// A default namign strategy for JustSaying bus.
    /// Topic names are defaulted to message type name, lowercase (one topic per message type).
    /// Queue name is default to queue name.
    /// 
    /// Such configuration gives a queue for each IntoQueue configuration , and a queue is subcribed to multiple topics, where one topic per message.
    /// </summary>
    class DefaultNamingStrategy : INamingStrategy
    {
        public string GetTopicName(string topicName, Type messageType) => messageType.ToTopicName().ToLowerInvariant();

        public string GetQueueName(SqsReadConfiguration sqsConfig, Type messageType)
            => string.IsNullOrWhiteSpace(sqsConfig.BaseQueueName)
                ? messageType.ToTopicName().ToLowerInvariant()
                : sqsConfig.BaseQueueName.ToLowerInvariant();
    }
}
