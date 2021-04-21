namespace JustSaying.AwsTools.MessageHandling
{
    /// <summary>
    /// Represents an Amazon SQS Queue with an ARN, that can be used to create SNS subscriptions.
    /// </summary>
    public interface ISqsQueueWithArn : ISqsQueue
    {
        /// <summary>
        /// Gets the ARN of this queue.
        /// </summary>
        string Arn { get; }
    }
}
