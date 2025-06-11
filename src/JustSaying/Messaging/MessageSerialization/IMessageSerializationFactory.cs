using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Defines a factory for creating message body serializers.
/// </summary>
public interface IMessageBodySerializationFactory
{
    /// <summary>
    /// Gets a serializer for messages of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of message for which to get a serializer. Must inherit from <see cref="Message"/>.</typeparam>
    /// <returns>An <see cref="IMessageBodySerializer"/> capable of serializing and deserializing messages of type <typeparamref name="T"/>.</returns>
    IMessageBodySerializer GetSerializer<T>() where T : Message;
}
