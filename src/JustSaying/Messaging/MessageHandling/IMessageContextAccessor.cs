namespace JustSaying.Messaging.MessageHandling;

public interface IMessageContextAccessor
{
    /// <summary>
    /// Gets or sets the context metadata about the SQS message currently being processed.
    /// </summary>
    MessageContext MessageContext { get; set; }
}