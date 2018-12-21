namespace JustSaying.Messaging.MessageHandling
{
    public interface IMessageContextAccessor
    {
        /// <summary>
        /// Get or set the context metadata about the SQS message currently being processed
        /// </summary>
        MessageContext MessageContext { get; set; }
    }
}
