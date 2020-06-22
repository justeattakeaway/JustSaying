using System;
using Amazon.SQS;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.AwsTools.MessageHandling
{
    /// <summary>
    /// Represents an Amazon SQS Queue
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
        /// Gets the SQS queue client.
        /// </summary>
        IAmazonSQS Client { get; }
    }
}
