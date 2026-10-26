using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging;

/// <summary>
/// The information available about an inbound message when resolving which message type it represents,
/// for example on a queue that carries more than one message type.
/// </summary>
public sealed class MessageDiscriminationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageDiscriminationContext"/> class.
    /// </summary>
    /// <param name="body">The unwrapped, decompressed message body.</param>
    /// <param name="subject">The SNS <c>Subject</c>, if present; otherwise <see langword="null"/>.</param>
    /// <param name="messageAttributes">The message attributes.</param>
    public MessageDiscriminationContext(string body, string subject, MessageAttributes messageAttributes)
    {
        Body = body;
        Subject = subject;
        MessageAttributes = messageAttributes;
    }

    /// <summary>
    /// Gets the unwrapped, decompressed message body (for example a CloudEvents envelope).
    /// </summary>
    public string Body { get; }

    /// <summary>
    /// Gets the SNS <c>Subject</c>, if present; otherwise <see langword="null"/>.
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// Gets the attributes associated with the message.
    /// </summary>
    public MessageAttributes MessageAttributes { get; }
}
