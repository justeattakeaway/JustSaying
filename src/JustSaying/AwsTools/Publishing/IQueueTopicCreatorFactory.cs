using System.Collections.Generic;
using Amazon;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.Publishing
{
    public interface IQueueTopicCreatorFactory
    {
        ITopicCreator CreateSnsCreator(string topicName, bool throwOnPublishFailure, IDictionary<string, string> tags);
        IQueueCreator CreateSqsCreator(string queueName,
            string region,
            int retryCountBeforeSendingToErrorQueue,
            Dictionary<string, string> tags);
    }
}
