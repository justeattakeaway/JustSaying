namespace JustSaying.Messaging.MessageHandling
{
    public interface IMessageContextReader
    {
        /// <summary>
        /// Get the context metadata about the SQS message currently being processed
        /// </summary>
        MessageContext MessageContext { get; }
    }
}
