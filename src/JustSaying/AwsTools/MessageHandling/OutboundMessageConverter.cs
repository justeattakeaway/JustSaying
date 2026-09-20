using System.Diagnostics;
using System.Text.Json.Nodes;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.Messaging;

internal sealed class OutboundMessageConverter : IOutboundMessageConverter
{
    private readonly PublishDestinationType _destinationType;
    private readonly IMessageBodySerializer _bodySerializer;
    private readonly MessageCompressionRegistry _compressionRegistry;
    private readonly PublishCompressionOptions _compressionOptions;
    private readonly string _subject;
    private readonly bool _isRawMessage;

    public OutboundMessageConverter(
        PublishDestinationType destinationType,
        IMessageBodySerializer bodySerializer,
        MessageCompressionRegistry compressionRegistry,
        PublishCompressionOptions compressionOptions,
        string subject,
        bool isRawMessage,
        int maximumMessageSize)
    {
        _destinationType = destinationType;
        _bodySerializer = bodySerializer;
        _compressionRegistry = compressionRegistry;
        _compressionOptions = compressionOptions;
        _subject = subject;
        _isRawMessage = isRawMessage;
        MaximumMessageSize = maximumMessageSize;
    }

    /// <inheritdoc />
    public int MaximumMessageSize { get; }

    public ValueTask<OutboundMessage> ConvertToOutboundMessageAsync(Message message, PublishMetadata publishMetadata, CancellationToken cancellationToken = default)
    {
        var serializedBody = _bodySerializer.Serialize(message);

        Dictionary<string, MessageAttributeValue> attributeValues = new();
        AddMessageAttributes(attributeValues, publishMetadata);
        InjectTraceContext(attributeValues);

        var messageBody = ApplyEnvelope(serializedBody);
        var messageSize = CalculateSize(messageBody, attributeValues);

        if (ShouldCompress(messageSize))
        {
            var compressionEncoding = _compressionOptions.CompressionEncoding;
            var compression = _compressionRegistry.GetCompression(compressionEncoding)
                ?? throw new InvalidOperationException($"No compression algorithm registered for encoding '{compressionEncoding}'.");

            var compressedAttributes = new Dictionary<string, MessageAttributeValue>(attributeValues)
            {
                [MessageAttributeKeys.ContentEncoding] = new() { DataType = "String", StringValue = compressionEncoding }
            };

            var compressedBody = ApplyEnvelope(compression.Compress(serializedBody));
            var compressedSize = CalculateSize(compressedBody, compressedAttributes);

            // Compression is not guaranteed to be a win. Base64 alone adds about a third, so an
            // incompressible payload comes out larger than it went in.
            if (compressedSize < messageSize)
            {
                messageBody = compressedBody;
                messageSize = compressedSize;
                attributeValues = compressedAttributes;
            }
        }

        if (messageSize > MaximumMessageSize)
        {
            throw new MessageTooLargeException(
                $"Message of type {message.GetType().FullName} is {messageSize} bytes, which exceeds the maximum of {MaximumMessageSize} bytes for this {(_destinationType == PublishDestinationType.Topic ? "topic" : "queue")}.")
            {
                MessageSize = messageSize,
                MaximumMessageSize = MaximumMessageSize
            };
        }

        return new ValueTask<OutboundMessage>(new OutboundMessage(messageBody, attributeValues, _subject));
    }

    /// <summary>
    /// Wraps a message body in the JustSaying envelope, where the destination requires one.
    /// </summary>
    /// <param name="body">The message body to wrap.</param>
    private string ApplyEnvelope(string body)
    {
        if (_destinationType != PublishDestinationType.Queue || _isRawMessage)
        {
            return body;
        }

        return new JsonObject
        {
            ["Message"] = body,
            ["Subject"] = _subject
        }.ToJsonString();
    }

    /// <summary>
    /// Calculates the size of a message as AWS will measure it when validating against the destination's limit.
    /// </summary>
    /// <param name="body">The message body, including any envelope.</param>
    /// <param name="attributes">The message attributes.</param>
    private int CalculateSize(string body, Dictionary<string, MessageAttributeValue> attributes)
    {
        // For a queue the subject travels inside the envelope, so it is already counted in the body.
        var subject = _destinationType == PublishDestinationType.Topic ? _subject : null;
        return MessagePayloadSize.Calculate(body, attributes, subject);
    }

    /// <summary>
    /// Determines whether a message of the given size should be compressed.
    /// </summary>
    /// <param name="messageSize">The size of the message, in bytes.</param>
    private bool ShouldCompress(int messageSize)
    {
        if (_compressionOptions?.CompressionEncoding is null || _compressionRegistry is null)
        {
            return false;
        }

        // The threshold says when compressing is worthwhile, but a message that will not fit has to be
        // compressed regardless, which matters when the threshold has been set above the destination's limit.
        return messageSize > MaximumMessageSize ||
               messageSize >= _compressionOptions.GetThresholdFor(MaximumMessageSize);
    }

    private static void InjectTraceContext(Dictionary<string, MessageAttributeValue> attributes)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        // Always produce a valid W3C traceparent, regardless of the runtime's default IdFormat.
        // activity.Id is only W3C-formatted when the OTel SDK has set Activity.DefaultIdFormat = W3C,
        // which is not guaranteed (e.g. metrics-only setup). Constructing it explicitly is always safe.
        var flags = activity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded) ? "01" : "00";
        var traceparent = activity.IdFormat == ActivityIdFormat.W3C
            ? activity.Id
            : $"00-{activity.TraceId}-{activity.SpanId}-{flags}";

        attributes[MessageAttributeKeys.TraceParent] = new MessageAttributeValue
        {
            DataType = "String",
            StringValue = traceparent
        };

        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            attributes[MessageAttributeKeys.TraceState] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = activity.TraceStateString
            };
        }
    }

    private static void AddMessageAttributes(Dictionary<string, MessageAttributeValue> requestMessageAttributes, PublishMetadata metadata)
    {
        if (metadata?.MessageAttributes == null || metadata.MessageAttributes.Count == 0)
        {
            return;
        }

        foreach (var attribute in metadata.MessageAttributes)
        {
            requestMessageAttributes.Add(attribute.Key, attribute.Value);
        }
    }
}
