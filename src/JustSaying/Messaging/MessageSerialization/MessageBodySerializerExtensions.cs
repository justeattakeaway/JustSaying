namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Helpers for adapting strongly-typed serializers to the type-erased internal boundary.
/// </summary>
internal static class MessageBodySerializerExtensions
{
    /// <summary>
    /// Adapts a strongly-typed <see cref="IMessageBodySerializer{TMessage}"/> to the type-erased
    /// <see cref="IMessageBodySerializer"/> used internally by the converters and dispatcher.
    /// </summary>
    public static IMessageBodySerializer Erase<TMessage>(this IMessageBodySerializer<TMessage> serializer) where TMessage : class
        => new ErasedMessageBodySerializer<TMessage>(serializer);
}
