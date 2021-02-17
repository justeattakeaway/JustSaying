using System.Collections.Generic;
using JustSaying.Messaging;

namespace JustSaying.AwsTools.Publishing
{
    public interface IMessagePublisherFactory
    {
        IMessagePublisher CreateSnsPublisher(string topicName, bool throwOnPublishFailure = false, IDictionary<string, string> tags = null);

        IMessagePublisher CreateSqsPublisher(string queueName, int retryCountBeforeSendingToErrorQueue);

    }
}
