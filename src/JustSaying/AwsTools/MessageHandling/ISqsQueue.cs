using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface ISqsQueue : IInterrogable
    {
        /// <summary>
        /// The name of the queue that operations on this <see cref="ISqsQueue"/> will be performed on
        /// </summary>
        string QueueName { get; }

        /// <summary>
        /// The system name of the region that this queue exists in
        /// </summary>
        string RegionSystemName { get; }

        /// <summary>
        /// The full URI of this queue
        /// </summary>
        Uri Uri { get; }

        /// <summary>
        /// Fetches messages from SQS, with a list of attributes to also load
        /// </summary>
        /// <param name="maximumCount">The maximum number of messages to get from SQS</param>
        /// <param name="requestMessageAttributeNames">A list of attributes to try and fetch for each message</param>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> to cancel the fetch</param>
        /// <returns></returns>
        Task<IList<Message>> GetMessagesAsync(int maximumCount, IEnumerable<string> requestMessageAttributeNames,
            CancellationToken stoppingToken = default);

        /// <summary>
        /// Updates a messages visibility timeout so that it won't be released to another subscriber before the timeout passes
        /// </summary>
        /// <param name="receiptHandle">The ReceiptHandle of the message to update (available from <see cref="Message"/>)</param>
        /// <param name="timeout">How far into the future this message should remain invisible to other consumers</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the visibility update</param>
        /// <returns></returns>
        Task ChangeMessageVisibilityAsync(string receiptHandle, TimeSpan timeout, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a message from SQS
        /// </summary>
        /// <param name="receiptHandle">The ReceiptHandle of the message to delete (available from <see cref="Message"/>)</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the deletion</param>
        /// <returns></returns>
        Task DeleteMessageAsync(string receiptHandle, CancellationToken cancellationToken = default);
    }
}
