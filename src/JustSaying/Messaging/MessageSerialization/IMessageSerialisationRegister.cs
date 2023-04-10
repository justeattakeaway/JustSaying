using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.MessageSerialization;

public interface IMessageSerializationRegister
{
    /// <summary>
    /// Deserializes a message.
    /// </summary>
    /// <param name="body">Message must always have Subject and Message properties</param>
    /// <returns>The <see cref="JustSaying.Models.Message"/> and <see cref="MessageAttributes"/>
    /// returned from the body of the SQS message.</returns>
    MessageWithAttributes DeserializeMessage(string body);

    /// <summary>
    /// Serializes a message for publishing
    /// </summary>
    /// <param name="message"></param>
    /// <param name="serializeForSnsPublishing">If set to false, then message will be wrapped in extra object with Subject and Message fields, e.g.:
    /// new { Subject = message.GetType().Name, Message = serializedMessage };
    ///
    /// AWS SNS service adds these automatically, so for publishing to topics don't add these properties
    /// </param>
    /// <returns>The serialized message for publishing.</returns>
    string Serialize(object message, bool serializeForSnsPublishing);

    /// <summary>
    /// Register a serializer for the given type, if one does not already exist.
    /// </summary>
    /// <typeparam name="TMessage">The type to register a serializer for.</typeparam>
    void AddSerializer<TMessage>() where TMessage : class;
}
