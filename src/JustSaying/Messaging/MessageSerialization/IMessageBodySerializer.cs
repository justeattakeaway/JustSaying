namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Defines the type-erased contract for serializing and deserializing message bodies, used
/// internally at the type-erased dispatch boundary. Custom serializers should implement the
/// strongly-typed <see cref="IMessageBodySerializer{TMessage}"/> instead.
/// </summary>
internal interface IMessageBodySerializer
{
    /// <summary>
    /// Serializes a message into a string representation.
    /// </summary>
    /// <param name="message">The message to serialize.</param>
    /// <returns>A string representation of the serialized message.</returns>
    string Serialize(object message);

    /// <summary>
    /// Deserializes a string representation back into a message object.
    /// </summary>
    /// <param name="message">The string representation of the message to deserialize.</param>
    /// <returns>The deserialized message object.</returns>
    object Deserialize(string message);
}
