using System.Collections.Concurrent;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

public class MessageSerializationRegister : IMessageSerializationRegister
{
    private readonly IMessageSubjectProvider _messageSubjectProvider;
    private readonly IMessageSerializationFactory _serializationFactory;
    private readonly IDictionary<Type, TypeSerializer> _map = new ConcurrentDictionary<Type, TypeSerializer>();

    public MessageSerializationRegister(IMessageSubjectProvider messageSubjectProvider, IMessageSerializationFactory serializationFactory)
    {
        _messageSubjectProvider = messageSubjectProvider ?? throw new ArgumentNullException(nameof(messageSubjectProvider));
        _serializationFactory = serializationFactory;
    }

    public void AddSerializer<T>() where T : Message
    {
        Type key = typeof(T);
        if (!_map.ContainsKey(key))
        {
            _map[key] = new TypeSerializer(typeof(T), _serializationFactory.GetSerializer<T>());
        }
    }

    public MessageWithAttributes DeserializeMessage(string body)
    {
        // Custom deserialisation from alternate payload into JustSaying payload

        // Can we remove this loop and simplify how this works?
        foreach (var pair in _map)
        {
            TypeSerializer typeSerializer = pair.Value;
            string messageSubject = typeSerializer.Serializer.GetMessageSubject(body); // Custom serializer pulls this from cloud event event type

            if (string.IsNullOrWhiteSpace(messageSubject))
            {
                continue;
            }

            Type matchedType = typeSerializer.Type;

            if (!string.Equals(_messageSubjectProvider.GetSubjectForType(matchedType), messageSubject, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var attributes = typeSerializer.Serializer.GetMessageAttributes(body);
            var message = typeSerializer.Serializer.Deserialize(body, matchedType);
            return new MessageWithAttributes(message, attributes);
        }

        var exception = new MessageFormatNotSupportedException("Message can not be handled - type undetermined.");

        // Put the message's body into the exception data so anyone catching
        // it can inspect it for other purposes, such as for logging.
        exception.Data["MessageBody"] = body;

        throw exception;
    }

    public string Serialize(Message message, bool serializeForSnsPublishing)
    {
        var messageType = message.GetType();

        if (!_map.TryGetValue(messageType, out TypeSerializer typeSerializer))
        {
            throw new MessageFormatNotSupportedException($"Failed to serialize message of type {messageType} because it is not registered for serialization.");
        }

        IMessageSerializer messageSerializer = typeSerializer.Serializer;
        return messageSerializer.Serialize(message, serializeForSnsPublishing, _messageSubjectProvider.GetSubjectForType(messageType));
    }
}
