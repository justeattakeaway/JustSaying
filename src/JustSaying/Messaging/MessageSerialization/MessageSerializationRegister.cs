using System.Collections.Concurrent;

namespace JustSaying.Messaging.MessageSerialization;

public class MessageSerializationRegister : IMessageSerializationRegister
{
    private readonly IMessageSubjectProvider _messageSubjectProvider;
    private readonly IMessageSerializationFactory _serializationFactory;
    private readonly ConcurrentDictionary<string, Lazy<TypeSerializer>> _typeSerializersBySubject = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<IMessageSerializer> _messageSerializers = new();

    public MessageSerializationRegister(IMessageSubjectProvider messageSubjectProvider, IMessageSerializationFactory serializationFactory)
    {
        _messageSubjectProvider = messageSubjectProvider ?? throw new ArgumentNullException(nameof(messageSubjectProvider));
        _serializationFactory = serializationFactory;
    }

    public void AddSerializer<TMessage>() where TMessage : class
    {
        string key = _messageSubjectProvider.GetSubjectForType(typeof(TMessage));

        var typeSerializer = _typeSerializersBySubject.GetOrAdd(key,
            _ => new Lazy<TypeSerializer>(
                () => new TypeSerializer(typeof(TMessage), _serializationFactory.GetSerializer<TMessage>())
            )
        ).Value;

        _messageSerializers.Add(typeSerializer.Serializer);
    }

    public MessageWithAttributes DeserializeMessage(string body)
    {
        // TODO Can we remove this loop rather than try each serializer?
        foreach (var messageSerializer in _messageSerializers)
        {
            string messageSubject = messageSerializer.GetMessageSubject(body); // Custom serializer pulls this from cloud event type

            if (string.IsNullOrWhiteSpace(messageSubject))
            {
                continue;
            }

            if (!_typeSerializersBySubject.TryGetValue(messageSubject, out var lazyTypeSerializer))
            {
                continue;
            }

            TypeSerializer typeSerializer = lazyTypeSerializer.Value;
            var attributes = typeSerializer.Serializer.GetMessageAttributes(body);
            var message = typeSerializer.Serializer.Deserialize(body, typeSerializer.Type);
            return new MessageWithAttributes(message, attributes);
        }

        var exception = new MessageFormatNotSupportedException("Message can not be handled - type undetermined.");

        // Put the message's body into the exception data so anyone catching
        // it can inspect it for other purposes, such as for logging.
        exception.Data["MessageBody"] = body;

        throw exception;
    }

    public string Serialize<TMessage>(TMessage message, bool serializeForSnsPublishing) where TMessage : class
    {
        var messageType = message.GetType();
        string subject = _messageSubjectProvider.GetSubjectForType(messageType);

        if (!_typeSerializersBySubject.TryGetValue(subject, out var lazyTypeSerializer))
        {
            throw new MessageFormatNotSupportedException($"Failed to serialize message of type {messageType} because it is not registered for serialization.");
        }

        var typeSerializer = lazyTypeSerializer.Value;
        IMessageSerializer messageSerializer = typeSerializer.Serializer;
        return messageSerializer.Serialize(message, serializeForSnsPublishing, subject);
    }
}
