using System.Collections.Generic;
using JustSaying.Messaging;

namespace JustSaying.AwsTools.Publishing
{
    /// <summary>
    /// This interface facilitates the creation of message publishers, without having to know the underlying types.
    /// </summary>
    public interface IMessagePublisherFactory
    {
        /// <summary>
        /// Returns an IMessagePublisher that publishes to a given topic.
        /// </summary>
        /// <param name="topicName">The name of the topic to publish to.</param>
        /// <param name="throwOnPublishFailure">Whether to throw an exception on failure publish.</param>
        /// <param name="tags">A list of tags that will be added to the topic if this publisher is used to create topics.</param>
        /// <returns>An <see cref="IMessagePublisher"/> that can be used to publish to this topic.</returns>
        IMessagePublisher GetSnsPublisher(string topicName, bool throwOnPublishFailure = false, IDictionary<string, string> tags = null);

        /// <summary>
        /// Returns an IMessagePublisher that publishes to a given queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to publish to.</param>
        /// <param name="retryCountBeforeSendingToErrorQueue">The number of times a message should be handled
        /// by subscribers to this queue before it is sent to the error queue. This will only be used if this publisher
        /// is used to create the queue.</param>
        /// <returns>An <see cref="IMessagePublisher"/> that can be used to publish to this queue.</returns>
        IMessagePublisher GetSqsPublisher(string queueName, int retryCountBeforeSendingToErrorQueue);

    }
}
