using System.Text;
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
    private readonly CloudEventFormatter _formatter;
    private readonly string _source;

    public CloudEventsMessageConverter(
        IMessageBodySerializationFactory serializationFactory,
        string source = "urn:justsaying")
    {
        _serializationFactory = serializationFactory ?? throw new ArgumentNullException(nameof(serializationFactory));
        _formatter = new JsonEventFormatter();
        _source = source;
    }

    /// <summary>
    /// Converts a JustSaying Message to a CloudEvent.
    /// </summary>
    public CloudEvent ToCloudEvent(Message message, PublishMetadata metadata = null)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();
        var serializer = _serializationFactory.GetSerializer(messageType);
        var messageBody = serializer.Serialize(message);

        var cloudEvent = new CloudEvent
        {
            Id = message.Id.ToString(),
            Type = messageType.FullName,
            Source = new Uri(_source),
            Time = message.TimeStamp,
            DataContentType = "application/json",
            Data = messageBody,
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
    public Message FromCloudEvent(CloudEvent cloudEvent)
    {
        if (cloudEvent == null)
            throw new ArgumentNullException(nameof(cloudEvent));

        if (string.IsNullOrEmpty(cloudEvent.Type))
            throw new InvalidOperationException("CloudEvent Type is required to deserialize message.");

        // Get the message type from the CloudEvent type attribute
        var messageType = Type.GetType(cloudEvent.Type);
        if (messageType == null)
        {
            throw new InvalidOperationException($"Could not resolve message type '{cloudEvent.Type}'.");
        }

        // Get the serializer for this message type
        var serializer = _serializationFactory.GetSerializer(messageType);

        // Extract the message data
        string messageBody;
        if (cloudEvent.Data is string strData)
        {
            messageBody = strData;
        }
        else if (cloudEvent.Data is byte[] bytesData)
        {
            messageBody = Encoding.UTF8.GetString(bytesData);
        }
        else
        {
            messageBody = System.Text.Json.JsonSerializer.Serialize(cloudEvent.Data);
        }

        // Deserialize to the actual message type
        var message = serializer.Deserialize(messageBody);

        // Restore CloudEvent metadata to Message properties
        if (Guid.TryParse(cloudEvent.Id, out var id))
        {
            message.Id = id;
        }

        if (cloudEvent.Time.HasValue)
        {
            message.TimeStamp = cloudEvent.Time.Value.UtcDateTime;
        }

        // Restore custom attributes
        if (cloudEvent.GetPopulatedAttributes().Any(a => a.Key.Name == "raisingcomponent"))
        {
            message.RaisingComponent = cloudEvent["raisingcomponent"]?.ToString();
        }

        if (cloudEvent.GetPopulatedAttributes().Any(a => a.Key.Name == "tenant"))
        {
            message.Tenant = cloudEvent["tenant"]?.ToString();
        }

        if (cloudEvent.GetPopulatedAttributes().Any(a => a.Key.Name == "conversation"))
        {
            message.Conversation = cloudEvent["conversation"]?.ToString();
        }

        return message;
    }

    /// <summary>
    /// Serializes a CloudEvent to bytes for Kafka.
    /// </summary>
    public byte[] Serialize(CloudEvent cloudEvent)
    {
        return _formatter.EncodeStructuredModeMessage(cloudEvent, out _);
    }

    /// <summary>
    /// Deserializes a CloudEvent from Kafka bytes.
    /// </summary>
    public CloudEvent Deserialize(byte[] data, string contentType = "application/cloudevents+json")
    {
        return _formatter.DecodeStructuredModeMessage(data, new System.Net.Mime.ContentType(contentType), null);
    }

    private IMessageBodySerializer GetSerializer(Type messageType)
    {
        var method = typeof(IMessageBodySerializationFactory)
            .GetMethod(nameof(IMessageBodySerializationFactory.GetSerializer))
            ?.MakeGenericMethod(messageType);

        return (IMessageBodySerializer)method?.Invoke(_serializationFactory, null);
    }
}
