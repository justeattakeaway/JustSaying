namespace JustSaying.Messaging.MessageHandling;

public interface IMessageContextReader
{
    /// <summary>
    /// Gets the context metadata about the SQS message currently being processed.
    /// </summary>
    MessageContext MessageContext { get; }
}