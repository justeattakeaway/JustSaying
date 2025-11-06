namespace JustSaying.Messaging.Middleware.Receive
{
    /// <summary>
    /// GetMessagesContext contains the parameters required to get messages from <see cref="ISqsQueue"/>.
    /// </summary>
    public sealed class ReceiveMessagesContext
    {
        /// <summary>
        /// Gets the maximum number of messages to return from the queue.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets the name of the SQS queue to get messages from.
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Gets the region of the SQS queue.
        /// </summary>
        public string RegionName { get; set; }
    }
}
