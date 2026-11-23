namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Defines a factory for creating message body serializers.
/// </summary>
public interface IMessageBodySerializationFactory
{
    /// <summary>
    /// Gets a serializer for messages of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of message for which to get a serializer.</typeparam>
    /// <returns>An <see cref="IMessageBodySerializer{T}"/> capable of serializing and deserializing messages of type <typeparamref name="T"/>.</returns>
    IMessageBodySerializer<T> GetSerializer<T>() where T : class;
}
