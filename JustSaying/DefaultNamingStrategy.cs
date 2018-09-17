using System;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;

namespace JustSaying
{
    /// <summary>
    /// A default naming strategy for JustSaying bus.
    /// Topic names are defaulted to message type name, lowercase (one topic per message type).
    /// Queue name is default to queue name.
    /// 
    /// Such configuration gives a queue for each IntoQueue configuration , and a queue is subcribed to multiple topics, where one topic per message.
    /// </summary>
    class DefaultNamingStrategy : INamingStrategy
    {
        public string GetTopicName(string baseTopicName, Type messageType)
            => string.IsNullOrEmpty(baseTopicName)
                ? messageType.ToTopicName().ToLower()
                : baseTopicName;

        public string GetQueueName(SqsReadConfiguration sqsConfig, Type messageType)
            => string.IsNullOrWhiteSpace(sqsConfig.BaseQueueName)
                ? messageType.ToTopicName().ToLower()
                : sqsConfig.BaseQueueName.ToLower();
    }
}
