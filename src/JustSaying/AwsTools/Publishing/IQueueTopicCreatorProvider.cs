using System.Collections.Generic;
using Amazon;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.Publishing
{
    public interface IQueueTopicCreatorProvider
    {
        ITopicCreator GetSnsCreator(string topicName, bool throwOnPublishFailure, IDictionary<string, string> tags);
        IQueueCreator GetSqsCreator(string queueName,
            string region,
            int retryCountBeforeSendingToErrorQueue,
            Dictionary<string, string> tags);
    }
}
