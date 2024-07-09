using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using JustSaying.AwsTools;
using JustSaying.Messaging.Compression;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

public class MessageSerializationRegister : IMessageSerializationRegister
{
    private readonly IMessageSubjectProvider _messageSubjectProvider;
    private readonly IMessageSerializationFactory _serializationFactory;
    private readonly IMessageCompressionRegistry _compressionRegistry;
    private readonly ConcurrentDictionary<string, Lazy<TypeSerializer>> _typeSerializersBySubject = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<IMessageSerializer> _messageSerializers = new();

    public MessageSerializationRegister(IMessageSubjectProvider messageSubjectProvider,
        IMessageSerializationFactory serializationFactory) : this(messageSubjectProvider, serializationFactory, null)
    {
    }

    public MessageSerializationRegister(IMessageSubjectProvider messageSubjectProvider,
        IMessageSerializationFactory serializationFactory, IMessageCompressionRegistry compressionRegistry)
    {
        _messageSubjectProvider = messageSubjectProvider ?? throw new ArgumentNullException(nameof(messageSubjectProvider));
        _serializationFactory = serializationFactory;
        _compressionRegistry = compressionRegistry;
    }

    public void AddSerializer<T>() where T : Message
    {
        string key = _messageSubjectProvider.GetSubjectForType(typeof(T));

        var typeSerializer = _typeSerializersBySubject.GetOrAdd(key,
            _ => new Lazy<TypeSerializer>(
                () => new TypeSerializer(typeof(T), _serializationFactory.GetSerializer<T>())
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
            // TODO This is bad and needs designing around...
            var contentEncoding = attributes.Get(MessageAttributeKeys.ContentEncoding);
            if (contentEncoding is not null)
            {
                var decompressor = _compressionRegistry.GetCompression(contentEncoding.StringValue);
                if (decompressor is null)
                {
                    throw new InvalidOperationException($"Compression encoding '{contentEncoding.StringValue}' is not registered.");
                }

                using var document = JsonDocument.Parse(body);
                JsonElement element = document.RootElement.GetProperty("Message");
                string json = element.ToString();

                var decompressedBody = decompressor.Decompress(json);

                using var memoryStream = new MemoryStream();
                using (var jsonWriter = new Utf8JsonWriter(memoryStream))
                {
                    jsonWriter.WriteStartObject();
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        if (property.Name == "Message")
                        {
                            jsonWriter.WritePropertyName("Message");
                            jsonWriter.WriteStringValue(decompressedBody);
                        }
                        else
                        {
                            property.WriteTo(jsonWriter);
                        }
                    }
                    jsonWriter.WriteEndObject();
                }

                // Update body with the new document string
                body = Encoding.UTF8.GetString(memoryStream.ToArray());

            }

            var message = typeSerializer.Serializer.Deserialize(body, typeSerializer.Type);
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
