using System;
using Amazon.SQS;
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
        /// The SQS Queue
        /// </summary>
        IAmazonSQS Client { get; }
    }
}
