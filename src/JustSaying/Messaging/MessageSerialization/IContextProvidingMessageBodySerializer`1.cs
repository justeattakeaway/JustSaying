using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// An optional capability of an <see cref="IMessageBodySerializer{TMessage}"/> whose wire format is
/// an envelope carrying metadata beyond the message payload itself. When a subscription's serializer
/// implements this interface, the metadata extracted while deserializing the body is used to build a
/// derived <see cref="MessageContext"/>, which handlers can observe through
/// <see cref="IMessageContextReader"/>.
/// </summary>
/// <typeparam name="TMessage">The type of the message to deserialize.</typeparam>
public interface IContextProvidingMessageBodySerializer<TMessage> where TMessage : class
{
    /// <summary>
    /// Deserializes a string representation back into a message object, also capturing a factory for
    /// the <see cref="MessageContext"/> describing the envelope the message arrived in.
    /// </summary>
    /// <param name="message">The string representation of the message to deserialize.</param>
    /// <param name="contextFactory">
    /// When this method returns, a factory that creates the <see cref="MessageContext"/> for this
    /// message, or <see langword="null"/> to use the default context.
    /// </param>
    /// <returns>The deserialized message object.</returns>
    TMessage Deserialize(string message, out MessageContextFactory contextFactory);
}
