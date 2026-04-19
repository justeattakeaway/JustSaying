#if NETFRAMEWORK
using System.Runtime.Serialization;
#endif

namespace JustSaying.AwsTools.MessageHandling;

/// <summary>
/// Represents errors that occur publishing a batch of messages.
/// </summary>
#if NETFRAMEWORK
[Serializable]
#endif
public class PublishBatchException : PublishException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PublishBatchException"/> class.
    /// </summary>
    public PublishBatchException()
        : base("Failed to publish batch of messages")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishBatchException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public PublishBatchException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishBatchException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception, if any.</param>
    public PublishBatchException(string message, Exception inner)
        : base(message, inner)
    {
    }

#if NETFRAMEWORK
    /// <summary>
    /// Initializes a new instance of the <see cref="PublishBatchException"/> class.
    /// </summary>
    /// <param name="info">
    /// The <see cref="SerializationInfo"/> that holds the serialized object data
    /// about the exception being thrown.
    /// </param>
    /// <param name="context">
    /// The <see cref="StreamingContext"/> that contains contextual information about the source or destination.
    /// </param>
    protected PublishBatchException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
#endif
}
