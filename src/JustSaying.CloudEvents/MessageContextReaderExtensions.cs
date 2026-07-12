using JustSaying.Messaging.MessageHandling;

namespace JustSaying.CloudEvents;

/// <summary>
/// CloudEvents-related extensions for <see cref="IMessageContextReader"/>.
/// </summary>
public static class MessageContextReaderExtensions
{
    /// <summary>
    /// Gets the <see cref="CloudEventMessageContext"/> for the message currently being processed, or
    /// <see langword="null"/> if there is no current message or it did not arrive as a CloudEvents
    /// envelope.
    /// </summary>
    /// <param name="reader">The <see cref="IMessageContextReader"/> to read the context from.</param>
    /// <returns>The <see cref="CloudEventMessageContext"/>, or <see langword="null"/>.</returns>
    public static CloudEventMessageContext GetCloudEventContext(this IMessageContextReader reader)
    {
        if (reader is null) throw new ArgumentNullException(nameof(reader));
        return reader.MessageContext as CloudEventMessageContext;
    }
}
