using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
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

        public Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken);
        public Task TagQueueAsync(string queueUrl, Dictionary<string, string> tags, CancellationToken cancellationToken);
        public Task<IList<Message>> ReceiveMessagesAsync(
            string queueUrl,
            int maxNumOfMessages,
            int secondsWaitTime,
            IList<string> attributesToLoad,
            CancellationToken cancellationToken);

        public Task ChangeMessageVisibilityAsync(
            string queueUrl,
            string receiptHandle,
            int visibilityTimeoutInSeconds,
            CancellationToken cancellationToken);

    }
}
