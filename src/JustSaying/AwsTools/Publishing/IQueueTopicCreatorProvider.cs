using System.Collections.Generic;
using Amazon;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.Publishing
{
    /// <summary>
    /// Provides <see cref="ITopicCreator"/> and <see cref="IQueueCreator"/>'s that may be used to create infrastructure,
    /// but cannot be published to.
    /// </summary>
    public interface IQueueTopicCreatorProvider
    {
        /// <summary>
        /// Returns an <see cref="ITopicCreator"/> that may be used to ensure a topic exists.
        /// </summary>
        /// <param name="topicName">The name of the topic to create.</param>
        /// <param name="tags">Any tags that should be added to the resource if it is created.</param>
        /// <returns>An <see cref="ITopicCreator"/> that can be used to create topics.</returns>
        ITopicCreator GetSnsCreator(string topicName, IDictionary<string, string> tags);

        /// <summary>
        /// Returns an <see cref="IQueueCreator"/> that may be used to ensure a queue exists.
        /// </summary>
        /// <param name="queueName">The name of the queue to create.</param>
        /// <param name="region">The region that the queue should be created in.</param>
        /// <param name="retryCountBeforeSendingToErrorQueue">The number of times handlers of this queue
        /// should be allowed to fail before messages are sent to the error queue.</param>
        /// <param name="tags">Any tags that should be added to the resource if it is created.</param>
        /// <returns>An <see cref="IQueueCreator"/> that can be used to create queues.</returns>
        IQueueCreator GetSqsCreator(string queueName,
            string region,
            int retryCountBeforeSendingToErrorQueue,
            Dictionary<string, string> tags);
    }
}
