using System.Text;
using System.Text.Json;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.Extensions.Kafka.CloudEvents;

/// <summary>
/// Converts JustSaying Messages to and from CloudEvents format.
/// This ensures compatibility with existing Message types while supporting CloudEvents specification.
/// </summary>
public class CloudEventsMessageConverter
{
    private readonly IMessageBodySerializationFactory _serializationFactory;
    private readonly string _source;
    private readonly CloudEventFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudEventsMessageConverter"/> class.
    /// </summary>
    /// <param name="serializationFactory">The serialization factory for messages.</param>
    /// <param name="source">The CloudEvents source URN.</param>
    public CloudEventsMessageConverter(
        IMessageBodySerializationFactory serializationFactory,
        string source = "urn:justsaying")
    {
        _serializationFactory = serializationFactory ?? throw new ArgumentNullException(nameof(serializationFactory));
        _source = source;
        _formatter = new JsonEventFormatter();
    }

    /// <summary>
    /// Converts a JustSaying Message to a CloudEvent.
    /// </summary>
    public CloudEvent ToCloudEvent<T>(T message, PublishMetadata metadata = null) where T : Message
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = typeof(T);
        var serializer = _serializationFactory.GetSerializer<T>();
        var messageBody = serializer.Serialize(message);

        // Parse the JSON string to a JsonDocument so CloudEvents can properly serialize it
        // Otherwise, the string gets double-encoded
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(messageBody);
        var dataElement = jsonDoc.RootElement.Clone();

        var cloudEvent = new CloudEvent
        {
            Id = message.Id.ToString(),
            Type = messageType.FullName,
            Source = new Uri(_source),
            Time = message.TimeStamp,
            DataContentType = "application/json",
            Data = dataElement,
            Subject = messageType.Name
        };

        // Add custom attributes from Message
        if (!string.IsNullOrEmpty(message.RaisingComponent))
            cloudEvent["raisingcomponent"] = message.RaisingComponent;

        if (!string.IsNullOrEmpty(message.Tenant))
            cloudEvent["tenant"] = message.Tenant;

        if (!string.IsNullOrEmpty(message.Conversation))
            cloudEvent["conversation"] = message.Conversation;

        // Add metadata attributes if provided
        if (metadata?.MessageAttributes != null)
        {
            foreach (var attr in metadata.MessageAttributes)
            {
                if (attr.Value?.StringValue != null)
                {
                    cloudEvent[$"attr_{attr.Key}"] = attr.Value.StringValue;
                }
            }
        }

        return cloudEvent;
    }

    /// <summary>
    /// Converts a CloudEvent to a JustSaying Message.
    /// </summary>
    public T FromCloudEvent<T>(CloudEvent cloudEvent) where T : Message
    {
        if (cloudEvent == null)
            throw new ArgumentNullException(nameof(cloudEvent));

        // Get the serializer for this message type
        var serializer = _serializationFactory.GetSerializer<T>();

        // Extract the message data - the data field contains the serialized message JSON
        string messageBody;
        
        if (cloudEvent.Data is string strData)
        {
            // Already a string - use it directly
            messageBody = strData;
        }
        else if (cloudEvent.Data is byte[] bytesData)
        {
            messageBody = Encoding.UTF8.GetString(bytesData);
        }
        else if (cloudEvent.Data is System.Text.Json.JsonElement jsonElement)
        {
            // JsonElement - need to check if it's a string value or an object
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                // It's a JSON string value - get the actual string content
                messageBody = jsonElement.GetString();
            }
            else
            {
                // It's a JSON object - get the raw text
                messageBody = jsonElement.GetRawText();
            }
        }
        else if (cloudEvent.Data != null)
        {
            // Other object type - serialize it
            messageBody = JsonSerializer.Serialize(cloudEvent.Data);
        }
        else
        {
            throw new InvalidOperationException("CloudEvent data is null");
        }
        
        // Deserialize the JSON string to the message type
        var message = serializer.Deserialize(messageBody);

        return (T)message;
    }

    /// <summary>
    /// Serializes a CloudEvent to bytes for Kafka (using JSON).
    /// </summary>
    public byte[] Serialize(CloudEvent cloudEvent)
    {
        var bytes = _formatter.EncodeStructuredModeMessage(cloudEvent, out _);
        return bytes.ToArray();
    }

    /// <summary>
    /// Deserializes a CloudEvent from Kafka bytes.
    /// </summary>
    public CloudEvent Deserialize(byte[] data)
    {
        return _formatter.DecodeStructuredModeMessage(data, null, null);
    }
}
