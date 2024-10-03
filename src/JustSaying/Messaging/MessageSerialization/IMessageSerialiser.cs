using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Defines the contract for serializing and deserializing message bodies.
/// </summary>
public interface IMessageBodySerializer
{
    /// <summary>
    /// Serializes a message into a string representation.
    /// </summary>
    /// <param name="message">The message to serialize.</param>
    /// <returns>A string representation of the serialized message.</returns>
    string Serialize(Message message);

    /// <summary>
    /// Deserializes a string representation back into a message object.
    /// </summary>
    /// <param name="message">The string representation of the message to deserialize.</param>
    /// <returns>The deserialized message object.</returns>
    Message Deserialize(string message);
}
