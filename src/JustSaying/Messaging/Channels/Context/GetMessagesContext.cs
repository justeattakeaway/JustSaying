namespace JustSaying.Messaging.Channels.Context
{
    /// <summary>
    /// GetMessagesContext contains the parameters required to get messages from <see cref="ISqsQueue"/>.
    /// </summary>
    public sealed class GetMessagesContext
    {
        /// <summary>
        /// The number of messages to get.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// The name of the SQS queue to get messages from.
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// The region of the SQS queue.
        /// </summary>
        public string RegionName { get; set; }
    }
}
