namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Defines the contract for serializing and deserializing message bodies of a specific type.
/// </summary>
/// <typeparam name="TMessage">The type of the message to serialize and deserialize.</typeparam>
public interface IMessageBodySerializer<TMessage> where TMessage : class
{
    /// <summary>
    /// Serializes a message into a string representation.
    /// </summary>
    /// <param name="message">The message to serialize.</param>
    /// <returns>A string representation of the serialized message.</returns>
    string Serialize(TMessage message);

    /// <summary>
    /// Deserializes a string representation back into a message object.
    /// </summary>
    /// <param name="message">The string representation of the message to deserialize.</param>
    /// <returns>The deserialized message object.</returns>
    TMessage Deserialize(string message);
}
