using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.Messaging.MessageHandling;

/// <summary>
/// Context metadata about the SQS message currently being processed.
/// </summary>
/// <remarks>
/// Creates an instance of <see cref="MessageContext"/>.
/// </remarks>
/// <param name="message">The <see cref="Amazon.SQS.Model.Message"/> currently being processed.</param>
/// <param name="queueUri">The URI of the SQS queue the message is from.</param>
/// <param name="messageAttributes">The <see cref="MessageAttributes"/> from the message.</param>
public class MessageContext(SQSMessage message, Uri queueUri, MessageAttributes messageAttributes)
{

    /// <summary>
    /// Gets the AWS SQS Message that is currently being processed.
    /// </summary>
    public SQSMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));

    /// <summary>
    /// Gets the SQS Queue that the message was received on.
    /// </summary>
    public Uri QueueUri { get; } = queueUri ?? throw new ArgumentNullException(nameof(queueUri));

    /// <summary>
    /// Gets a collection of <see cref="MessageAttributeValue"/>'s that were sent with this message.
    /// </summary>
    public MessageAttributes MessageAttributes { get; } = messageAttributes ?? throw new ArgumentNullException(nameof(messageAttributes));
}