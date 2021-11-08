using Amazon.SQS.Model;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.AwsTools.MessageHandling
{
    /// <summary>
    /// Represents an Amazon SQS Queue.
    /// </summary>
    public interface ISqsQueue : IInterrogable
    {
        /// <summary>
        /// Gets the name of the queue that operations on this <see cref="ISqsQueue"/> will be performed on.
        /// </summary>
        string QueueName { get; }

        /// <summary>
        /// Gets the system name of the region that this queue exists in.
        /// </summary>
        string RegionSystemName { get; }

        /// <summary>
        /// Gets the absolute URI of this queue.
        /// </summary>
        Uri Uri { get; }

        /// <summary>
        /// Gets the ARN of this queue.
        /// </summary>
        string Arn { get; }

        /// <summary>
        /// Deletes a message from a queue.
        /// </summary>
        /// <param name="queueUrl">The URL of the queue to delete a message from.</param>
        /// <param name="receiptHandle">The receipt handle of the message to delete.</param>
        /// <param name="cancellationToken">A cancellation token to cancel this operation.</param>
        /// <returns>A <see cref="Task"/> will complete when the message has been deleted, or the task has faulted.</returns>
        Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken);

        /// <summary>
        /// Tags a queue with one or more key-value pairs.
        /// </summary>
        /// <param name="queueUrl">The URL of the queue to tag a message from.</param>
        /// <param name="tags">A <see cref="Dictionary{string, string}"/> of tags to tag this queue with.</param>
        /// <param name="cancellationToken">A cancellation token to cancel this operation.</param>
        /// <returns>A <see cref="Task"/> will complete when the queue has been tagged, or the task has faulted.</returns>
        Task TagQueueAsync(string queueUrl, Dictionary<string, string> tags, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads messages from a queue.
        /// </summary>
        /// <param name="queueUrl">The URL of the queue to download a message from.</param>
        /// <param name="maxNumOfMessages">The maximum number of messages to try to download. Values larger than 10 will default to 10. </param>
        /// <param name="secondsWaitTime">The number of seconds to wait for messages to be available before returning.</param>
        /// <param name="attributesToLoad">A list of attributes to retrieve for the downloaded messages.</param>
        /// <param name="cancellationToken">A cancellation token to cancel this operation.</param>
        /// <returns>A <see cref="Task"/> will complete when messages have been received, or the task has faulted.</returns>
        Task<IList<Message>> ReceiveMessagesAsync(
            string queueUrl,
            int maxNumOfMessages,
            int secondsWaitTime,
            IList<string> attributesToLoad,
            CancellationToken cancellationToken);

        /// <summary>
        /// Updates the visibility timeout of a message to prevent it from being re-handled until a future time.
        /// </summary>
        /// <param name="queueUrl">The URL of the queue to delete a message from</param>
        /// <param name="receiptHandle">The receipt handle of the message that will be updated</param>
        /// <param name="visibilityTimeoutInSeconds">The number of seconds until this message will be visible to other consumers</param>
        /// <param name="cancellationToken">A cancellation token to cancel this operation.</param>
        /// <returns></returns>
        Task ChangeMessageVisibilityAsync(
            string queueUrl,
            string receiptHandle,
            int visibilityTimeoutInSeconds,
            CancellationToken cancellationToken);

    }
}
