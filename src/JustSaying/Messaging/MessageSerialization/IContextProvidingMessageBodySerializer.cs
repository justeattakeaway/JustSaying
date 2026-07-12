namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// The type-erased counterpart of <see cref="IContextProvidingMessageBodySerializer{TMessage}"/>,
/// used internally at the dispatch boundary.
/// </summary>
internal interface IContextProvidingMessageBodySerializer
{
    /// <summary>
    /// Deserializes a string representation back into a message object, also capturing a factory for
    /// the <see cref="MessageHandling.MessageContext"/> describing the envelope the message arrived
    /// in, or <see langword="null"/> to use the default context.
    /// </summary>
    object Deserialize(string message, out MessageHandling.MessageContextFactory contextFactory);
}
