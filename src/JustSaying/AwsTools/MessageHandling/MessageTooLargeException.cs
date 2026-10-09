#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace JustSaying.AwsTools.MessageHandling;

/// <summary>
/// The exception thrown when a message is too large for the destination it is being published to,
/// after any configured compression has been applied.
/// </summary>
/// <remarks>
/// This is thrown before the message reaches AWS, in preference to letting the service reject the
/// request with an opaque <c>InvalidParameter</c> error.
/// </remarks>
#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class MessageTooLargeException : PublishException
{
    public MessageTooLargeException() : base("The message is too large for the destination.")
    {
    }

    public MessageTooLargeException(string message) : base(message)
    {
    }

    public MessageTooLargeException(string message, Exception inner) : base(message, inner)
    {
    }

#if !NET8_0_OR_GREATER
    protected MessageTooLargeException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        MessageSize = info.GetInt32(nameof(MessageSize));
        MaximumMessageSize = info.GetInt32(nameof(MaximumMessageSize));
    }

    /// <inheritdoc />
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);

        info.AddValue(nameof(MessageSize), MessageSize);
        info.AddValue(nameof(MaximumMessageSize), MaximumMessageSize);
    }
#endif

    /// <summary>
    /// Gets or sets the size of the message, in bytes.
    /// </summary>
    public int MessageSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum size the destination accepts, in bytes.
    /// </summary>
    public int MaximumMessageSize { get; set; }
}
