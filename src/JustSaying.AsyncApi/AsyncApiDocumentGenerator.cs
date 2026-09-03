using System.Reflection;
using System.Text.Json;
using System.Text.Json.Schema;
using ByteBard.AsyncAPI.Models;
using JustSaying.CloudEvents;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Metadata;
using Microsoft.Extensions.Logging;

namespace JustSaying.AsyncApi;

/// <summary>
/// Generates an AsyncAPI 3.0 document from the publications and subscriptions captured in an
/// <see cref="IMessagingMetadataRegistry"/>.
/// </summary>
public sealed class AsyncApiDocumentGenerator
{
    private const string CloudEventsContentType = "application/cloudevents+json";

    private const string JsonContentType = "application/json";

    private readonly IMessagingMetadataRegistry _registry;
    private readonly AsyncApiOptions _options;
    private readonly IMessageBodySerializationFactory _serializationFactory;
    private readonly ILogger<AsyncApiDocumentGenerator> _logger;
    private readonly object _syncRoot = new();

    // Per-generation state: each registration is described once, and the serializer-derived
    // schema options and non-System.Text.Json warnings are computed once per serializer.
    private readonly Dictionary<MessageTypeMetadata, MessageDescription> _descriptions = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<JsonSerializerOptions, JsonSerializerOptions> _schemaOptions = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Type> _undescribableSerializers = [];
    private JsonSerializerOptions _fallbackSchemaOptions;
    private bool _fallbackSchemaOptionsResolved;

    static AsyncApiDocumentGenerator()
    {
        // ByteBard's writer materializes these enum arrays reflectively (Enum.GetValues, which
        // calls Array.CreateInstance): SchemaType[] for every schema "type" keyword and
        // ReferenceType[] when parsing "#/components/..." references. A Native AOT image only
        // contains array types that are constructed statically somewhere, so construct them
        // here or the writer throws NotSupportedException at runtime under Native AOT.
        _ = Enum.GetValues<SchemaType>();
        _ = Enum.GetValues<ReferenceType>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncApiDocumentGenerator"/> class.
    /// </summary>
    /// <param name="registry">The registry of captured publications and subscriptions.</param>
    /// <param name="options">The options configuring the generated document.</param>
    /// <param name="serializationFactory">
    /// The app-wide message body serialization factory, used to discover the payload wire contract of a
    /// registration whose own serializer was not captured. Each captured registration is described from
    /// the serializer it actually uses (see <see cref="MessageTypeMetadata.Serializer"/>).
    /// </param>
    /// <param name="logger">The logger used to surface why parts of the document are omitted.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="registry"/> or <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public AsyncApiDocumentGenerator(
        IMessagingMetadataRegistry registry,
        AsyncApiOptions options,
        IMessageBodySerializationFactory serializationFactory = null,
        ILogger<AsyncApiDocumentGenerator> logger = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _serializationFactory = serializationFactory;
        _logger = logger;
    }

    /// <summary>
    /// Generates the AsyncAPI document.
    /// </summary>
    /// <returns>The generated <see cref="AsyncApiDocument"/>.</returns>
    public AsyncApiDocument Generate()
    {
        // The per-serializer caches are shared across generations, so serialize them: generation is
        // rare (build time or a documentation request), and the registry is immutable by then.
        lock (_syncRoot)
        {
            return GenerateCore();
        }
    }

    private AsyncApiDocument GenerateCore()
    {
        var document = new AsyncApiDocument()
        {
            Id = _options.Id,
            Info = new AsyncApiInfo()
            {
                Title = _options.Title ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "JustSaying application",
                Version = _options.Version,
                Description = _options.Description,
            },
            DefaultContentType = JsonContentType,
        };

        string primaryRegion = PrimaryRegion();

        AddServers(document, primaryRegion);

        if (_registry.Publications.Count == 0 && _registry.Subscriptions.Count == 0)
        {
            _logger?.LogWarning(
                "The generated AsyncAPI document is empty: no publications or subscriptions were captured. " +
                "If the application does configure messaging, ensure AddJustSaying ran in the same service collection before the document was generated.");
        }

        var channels = new Dictionary<string, ChannelState>(StringComparer.Ordinal);
        var operationMessages = new Dictionary<string, List<MessageTypeMetadata>>(StringComparer.Ordinal);
        var envelopeOperations = new HashSet<string>(StringComparer.Ordinal);

        foreach (var publication in _registry.Publications)
        {
            if (publication.IsDynamic)
            {
                // A dynamic destination has no static address; there is no channel to document.
                _logger?.LogWarning(
                    "Publication of {MessageTypes} uses a dynamic destination name computed per message, so it has no static address and is omitted from the AsyncAPI document.",
                    Join(publication.Messages));
                continue;
            }

            var channel = AddChannel(
                document,
                channels,
                primaryRegion,
                publication.DestinationName,
                publication.DestinationKind,
                publication.Region ?? _registry.Region,
                $"The {publication.DestinationName} {(publication.DestinationKind == MessagingDestinationKind.SnsTopic ? "SNS topic" : "SQS queue")}.",
                publication.Messages);

            // Several publications can target the same destination with different message
            // types; the operation is rebuilt from the merged set so none are dropped.
            string operationKey = Sanitize($"send-{channel.Key}");
            var merged = MergeOperationMessages(operationMessages, operationKey, publication.Messages);

            if (publication.UsesQueueEnvelope)
            {
                envelopeOperations.Add(operationKey);
            }

            document.Operations[operationKey] = new AsyncApiOperation()
            {
                Action = AsyncApiAction.Send,
                Channel = new AsyncApiChannelReference($"#/channels/{channel.Key}"),
                Summary = $"Publish {Join(merged)} to {publication.DestinationName}.",
                Description = envelopeOperations.Contains(operationKey) ? QueueEnvelopeDescription : null,
                Messages = [.. merged.Select((m) => new AsyncApiMessageReference($"#/channels/{channel.Key}/messages/{channel.MessageKeys[WireName(m)]}"))],
            };
        }

        foreach (var subscription in _registry.Subscriptions)
        {
            var description = subscription.TopicName != null
                ? $"The {subscription.QueueName} SQS queue, subscribed to the {subscription.TopicName} SNS topic."
                : $"The {subscription.QueueName} SQS queue.";

            var channel = AddChannel(
                document,
                channels,
                primaryRegion,
                subscription.QueueName,
                MessagingDestinationKind.SqsQueue,
                subscription.Region ?? _registry.Region,
                description,
                subscription.Messages);

            string operationKey = Sanitize($"receive-{channel.Key}");
            var merged = MergeOperationMessages(operationMessages, operationKey, subscription.Messages);

            document.Operations[operationKey] = new AsyncApiOperation()
            {
                Action = AsyncApiAction.Receive,
                Channel = new AsyncApiChannelReference($"#/channels/{channel.Key}"),
                Summary = $"Receive {Join(merged)} from {subscription.QueueName}.",
                Description = DeliveryDescription(subscription),
                Messages = [.. merged.Select((m) => new AsyncApiMessageReference($"#/channels/{channel.Key}/messages/{channel.MessageKeys[WireName(m)]}"))],
            };
        }

        // Every message states its own content type; the document-level default is only a
        // convenience for readers, so it reflects the one format in use when there is one.
        var contentTypes = _descriptions.Values.Select((d) => d.ContentType).Distinct(StringComparer.Ordinal).ToList();
        if (contentTypes.Count == 1)
        {
            document.DefaultContentType = contentTypes[0];
        }

        _options.PostProcess?.Invoke(document);

        return document;
    }

    /// <summary>
    /// The region whose servers get the plain "sns"/"sqs" keys; destinations in any other
    /// region reference a region-suffixed server. This is the bus's configured region, or,
    /// when only explicitly-addressed destinations exist, their sole region.
    /// </summary>
    private string PrimaryRegion()
    {
        if (_registry.Region != null)
        {
            return _registry.Region;
        }

        var regions = _registry.Publications.Select((p) => p.Region)
            .Concat(_registry.Subscriptions.Select((s) => s.Region))
            .Where((r) => r != null)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return regions.Count == 1 ? regions[0] : null;
    }

    private void AddServers(AsyncApiDocument document, string primaryRegion)
    {
        var topicRegions = _registry.Publications
            .Where((p) => p.DestinationKind == MessagingDestinationKind.SnsTopic)
            .Select((p) => p.Region ?? _registry.Region)
            .Concat(_registry.Subscriptions.Where((s) => s.TopicName != null).Select((s) => s.Region ?? _registry.Region));

        var queueRegions = _registry.Publications
            .Where((p) => p.DestinationKind == MessagingDestinationKind.SqsQueue)
            .Select((p) => p.Region ?? _registry.Region)
            .Concat(_registry.Subscriptions.Select((s) => s.Region ?? _registry.Region));

        foreach (var region in topicRegions.Where((r) => r != null).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            document.Servers[ServerKey("sns", region, primaryRegion)] = new AsyncApiServer()
            {
                Host = $"sns.{region}.amazonaws.com",
                Protocol = "sns",
                Description = $"Amazon SNS in {region}.",
            };
        }

        foreach (var region in queueRegions.Where((r) => r != null).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            document.Servers[ServerKey("sqs", region, primaryRegion)] = new AsyncApiServer()
            {
                Host = $"sqs.{region}.amazonaws.com",
                Protocol = "sqs",
                Description = $"Amazon SQS in {region}.",
            };
        }
    }

    private static string ServerKey(string protocol, string region, string primaryRegion)
        => region == primaryRegion ? protocol : Sanitize($"{protocol}-{region}");

    private sealed record ChannelIdentity(string Address, MessagingDestinationKind Kind, string Region);

    private sealed class ChannelState(string key, AsyncApiChannel channel, ChannelIdentity identity)
    {
        public string Key { get; } = key;

        public AsyncApiChannel Channel { get; } = channel;

        public ChannelIdentity Identity { get; } = identity;

        /// <summary>
        /// Gets the key each message wire name was allocated within this channel, so that
        /// operation references point at the message the channel actually holds.
        /// </summary>
        public Dictionary<string, string> MessageKeys { get; } = new(StringComparer.Ordinal);
    }

    private ChannelState AddChannel(
        AsyncApiDocument document,
        Dictionary<string, ChannelState> channels,
        string primaryRegion,
        string address,
        MessagingDestinationKind kind,
        string region,
        string description,
        IReadOnlyList<MessageTypeMetadata> messages)
    {
        var identity = new ChannelIdentity(address, kind, region);
        string kindSuffix = kind == MessagingDestinationKind.SnsTopic ? "topic" : "queue";

        // A topic and a queue can share a name, the same name can exist in two regions, and
        // distinct addresses can sanitize to the same key; each is a different destination, so
        // the channel keys are kept distinct. A publication and subscription on the same queue
        // share an identity and reuse the one channel.
        List<string> candidates = [Sanitize(address), Sanitize($"{address}-{kindSuffix}")];
        if (region != null)
        {
            candidates.Add(Sanitize($"{address}-{kindSuffix}-{region}"));
        }

        string channelKey = AllocateKey(
            candidates,
            (key) => !channels.TryGetValue(key, out var existing) || existing.Identity == identity);

        if (!channels.TryGetValue(channelKey, out var state))
        {
            var channel = new AsyncApiChannel()
            {
                Address = address,
                Description = description,
            };

            if (region != null)
            {
                string serverKey = ServerKey(kind == MessagingDestinationKind.SnsTopic ? "sns" : "sqs", region, primaryRegion);
                if (document.Servers.ContainsKey(serverKey))
                {
                    channel.Servers.Add(new AsyncApiServerReference($"#/servers/{serverKey}"));
                }
            }

            document.Channels[channelKey] = channel;
            channels[channelKey] = state = new ChannelState(channelKey, channel, identity);
        }

        foreach (var message in messages)
        {
            string wireName = WireName(message);

            // Distinct wire names can sanitize to the same key, so each is allocated a key of
            // its own rather than overwriting the message already under that key.
            if (!state.MessageKeys.TryGetValue(wireName, out var messageKey))
            {
                messageKey = AllocateKey([Sanitize(wireName)], (key) => !state.Channel.Messages.ContainsKey(key));
                state.MessageKeys[wireName] = messageKey;
            }

            state.Channel.Messages[messageKey] = CreateMessage(message);
        }

        return state;
    }

    /// <summary>
    /// Returns the first candidate key that is available, or, when every candidate is taken,
    /// the last candidate with a deterministic numeric suffix appended until it is available.
    /// </summary>
    private static string AllocateKey(IReadOnlyList<string> candidates, Func<string, bool> isAvailable)
    {
        foreach (var candidate in candidates)
        {
            if (isAvailable(candidate))
            {
                return candidate;
            }
        }

        for (int i = 2; ; i++)
        {
            string candidate = $"{candidates[^1]}-{i}";
            if (isAvailable(candidate))
            {
                return candidate;
            }
        }
    }

    private List<MessageTypeMetadata> MergeOperationMessages(
        Dictionary<string, List<MessageTypeMetadata>> operationMessages,
        string operationKey,
        IReadOnlyList<MessageTypeMetadata> messages)
    {
        if (!operationMessages.TryGetValue(operationKey, out var merged))
        {
            operationMessages[operationKey] = merged = [];
        }

        foreach (var message in messages)
        {
            string wireName = WireName(message);
            if (!merged.Any((m) => WireName(m) == wireName))
            {
                merged.Add(message);
            }
        }

        return merged;
    }

    /// <summary>
    /// Describes how documented payloads are actually written to the queue. Without raw messages,
    /// JustSaying wraps the payload in its own queue envelope, so the SQS body is not the
    /// documented payload itself.
    /// </summary>
    private const string QueueEnvelopeDescription =
        "The publisher wraps each message in JustSaying's queue envelope: the SQS message body is " +
        "{ \"Message\": \"...\", \"Subject\": \"...\" }, and the documented message payload is the JSON-encoded string in its \"Message\" property. " +
        "Publish with raw messages to send the payload verbatim instead.";

    /// <summary>
    /// Describes how documented payloads actually arrive on the queue. Without raw message
    /// delivery, SNS wraps each message in its notification envelope, so the SQS body is not
    /// the documented payload itself.
    /// </summary>
    private static string DeliveryDescription(SubscriptionMetadata subscription)
    {
        if (subscription.TopicName == null)
        {
            return null;
        }

        return subscription.RawMessageDelivery
            ? "The topic subscription uses raw message delivery: the SQS message body is the documented message payload."
            : "The topic subscription does not use raw message delivery: the SQS message body is the Amazon SNS notification envelope, and the documented message payload is the JSON-encoded string in its \"Message\" property.";
    }

    private AsyncApiMessage CreateMessage(MessageTypeMetadata metadata)
    {
        var description = Describe(metadata);

        var message = new AsyncApiMessage()
        {
            Name = description.WireName,
            Title = FriendlyTypeName(description.PayloadType),
            ContentType = description.ContentType,
        };

        if (description.Payload != null)
        {
            message.Payload = description.Payload;
        }

        return message;
    }

    /// <summary>
    /// How a registration's message appears on the wire: the name it is identified by, the CLR
    /// type readers know it as, and the content type and schema of the body.
    /// </summary>
    private sealed record MessageDescription(string WireName, Type PayloadType, string ContentType, AsyncApiJsonSchema Payload);

    private string WireName(MessageTypeMetadata metadata) => Describe(metadata).WireName;

    /// <summary>
    /// Describes a registration from the serializer it actually uses. Serialization is configured
    /// per registration, so this — not any application-wide setting — is what determines whether a
    /// message is plain JSON or a CloudEvents envelope, and which options shape its schema.
    /// </summary>
    private MessageDescription Describe(MessageTypeMetadata metadata)
    {
        if (_descriptions.TryGetValue(metadata, out var description))
        {
            return description;
        }

        var body = DescribeBody(metadata.MessageType, metadata.Serializer);

        // A CloudEvent is identified by its `type`; anything else by its registered logical name.
        string wireName = (metadata.Serializer as ICloudEventMessageBodySerializer)?.Type
            ?? metadata.WireName
            ?? metadata.MessageType.Name;

        description = new MessageDescription(wireName, body.PayloadType, body.ContentType, body.Payload);
        _descriptions[metadata] = description;
        return description;
    }

    private (Type PayloadType, string ContentType, AsyncApiJsonSchema Payload) DescribeBody(Type messageType, object serializer)
    {
        switch (serializer)
        {
            case ICloudEventMessageBodySerializer cloudEvent:
                // The wire format is the CloudEvents structured-mode envelope with the payload under
                // "data"; documenting the bare payload schema would hand consumers the wrong shape.
                // Whether the handler sees the envelope or just the data is the same on the wire.
                var data = DescribeBody(cloudEvent.DataType, cloudEvent.DataSerializer);
                return (cloudEvent.DataType, CloudEventsContentType, CreateCloudEventEnvelopeSchema(cloudEvent, data.Payload));

            case ISystemTextJsonMessageBodySerializer systemTextJson:
                return (messageType, JsonContentType, ExportPayloadSchema(messageType, SchemaOptions(systemTextJson.SerializerOptions)));

            case null:
                // The registration's serializer was not captured; fall back to the app-wide factory.
                return (messageType, JsonContentType, ExportPayloadSchema(messageType, FallbackSchemaOptions()));

            default:
                return (messageType, JsonContentType, ExportPayloadSchema(messageType, UndescribableSerializerSchemaOptions(serializer)));
        }
    }

    private AsyncApiJsonSchema ExportPayloadSchema(Type messageType, JsonSerializerOptions serializerOptions)
    {
        if (serializerOptions == null)
        {
            return null;
        }

        try
        {
            var schemaNode = serializerOptions.GetJsonSchemaAsNode(messageType, new JsonSchemaExporterOptions
            {
                TreatNullObliviousAsNonNullable = true,
            });

            return JsonSchemaNodeMapper.Map(schemaNode);
        }
        catch (NotSupportedException exception)
        {
            // The serializer cannot describe this type; the message is documented without a payload schema.
            _logger?.LogWarning(
                "A payload schema for message type {MessageType} could not be derived ({Reason}); the message is documented without one.",
                messageType,
                exception.Message);
            return null;
        }
    }

    private static AsyncApiJsonSchema CreateCloudEventEnvelopeSchema(ICloudEventMessageBodySerializer serializer, AsyncApiJsonSchema dataSchema)
    {
        var typeSchema = new AsyncApiJsonSchema() { Type = SchemaType.String };
        if (serializer.Type != null)
        {
            typeSchema.Const = new AsyncApiAny(serializer.Type);
        }

        var dataContentTypeSchema = new AsyncApiJsonSchema() { Type = SchemaType.String };
        if (serializer.DataContentType != null)
        {
            dataContentTypeSchema.Const = new AsyncApiAny(serializer.DataContentType);
        }

        // Mirrors the envelope written by the CloudEvents serializers. Additional properties stay
        // allowed so that CloudEvents extension attributes remain valid.
        return new AsyncApiJsonSchema()
        {
            Type = SchemaType.Object,
            Description = $"A CloudEvents 1.0 structured-mode JSON envelope carrying {FriendlyTypeName(serializer.DataType)} in its \"data\" member.",
            Properties = new Dictionary<string, AsyncApiJsonSchema>(StringComparer.Ordinal)
            {
                ["specversion"] = new() { Type = SchemaType.String, Const = new AsyncApiAny("1.0") },
                ["id"] = new() { Type = SchemaType.String, MinLength = 1 },
                ["source"] = new() { Type = SchemaType.String, Format = "uri-reference" },
                ["type"] = typeSchema,
                ["time"] = new() { Type = SchemaType.String, Format = "date-time" },
                ["datacontenttype"] = dataContentTypeSchema,
                ["subject"] = new() { Type = SchemaType.String },
                ["data"] = dataSchema ?? new AsyncApiJsonSchema(),
            },
            Required = new HashSet<string>(StringComparer.Ordinal) { "specversion", "id", "source", "type", "data" },
        };
    }

    /// <summary>
    /// The options to export schemas with for a System.Text.Json serializer, honouring an explicit
    /// <see cref="AsyncApiOptions.SerializerOptions"/> override and ensuring a type info resolver.
    /// </summary>
    private JsonSerializerOptions SchemaOptions(JsonSerializerOptions serializerOptions)
    {
        if (_options.SerializerOptions != null)
        {
            serializerOptions = _options.SerializerOptions;
        }

        if (serializerOptions == null)
        {
            return null;
        }

        if (_schemaOptions.TryGetValue(serializerOptions, out var schemaOptions))
        {
            return schemaOptions;
        }

        schemaOptions = serializerOptions;

        if (serializerOptions.TypeInfoResolver == null)
        {
            // The schema exporter requires a resolver to be set explicitly; the serializers rely on
            // it being applied lazily. Under Native AOT there is no reflection resolver to fall back
            // to, so messages are documented without payload schemas.
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                _logger?.LogWarning(
                    "Reflection-based serialization is disabled and the serializer options have no TypeInfoResolver, so messages are documented without payload schemas. " +
                    "Use serializer options with a source-generated JsonSerializerContext to document payload schemas.");
                schemaOptions = null;
            }
            else
            {
#pragma warning disable IL2026, IL3050
                schemaOptions = new JsonSerializerOptions(serializerOptions)
                {
                    TypeInfoResolver = JsonSerializerOptions.Default.TypeInfoResolver,
                };
#pragma warning restore IL2026, IL3050
            }
        }

        _schemaOptions[serializerOptions] = schemaOptions;
        return schemaOptions;
    }

    /// <summary>
    /// A Newtonsoft or custom serializer's wire contract cannot be derived from System.Text.Json
    /// options (for example, JustSaying's Newtonsoft serializer writes enums as strings), so rather
    /// than documenting a schema that may not match the wire format, its messages are documented
    /// without payload schemas unless <see cref="AsyncApiOptions.SerializerOptions"/> is supplied.
    /// </summary>
    private JsonSerializerOptions UndescribableSerializerSchemaOptions(object serializer)
    {
        if (_options.SerializerOptions != null)
        {
            return SchemaOptions(_options.SerializerOptions);
        }

        if (_undescribableSerializers.Add(serializer.GetType()))
        {
            _logger?.LogWarning(
                "The message body serializer ({Serializer}) is not System.Text.Json-based, so the wire contract cannot be derived and its messages are documented without payload schemas. " +
                "Set AsyncApiOptions.SerializerOptions to options matching the wire format to document payload schemas.",
                serializer.GetType());
        }

        return null;
    }

    /// <summary>
    /// The options to export schemas with for a registration whose serializer was not captured,
    /// derived from the app-wide serialization factory.
    /// </summary>
    private JsonSerializerOptions FallbackSchemaOptions()
    {
        if (_fallbackSchemaOptionsResolved)
        {
            return _fallbackSchemaOptions;
        }

        _fallbackSchemaOptionsResolved = true;

        if (_options.SerializerOptions != null)
        {
            return _fallbackSchemaOptions = SchemaOptions(_options.SerializerOptions);
        }

        switch (_serializationFactory)
        {
            case SystemTextJsonSerializationFactory systemTextJsonFactory:
                return _fallbackSchemaOptions = SchemaOptions(systemTextJsonFactory.SerializerOptions);

            case null:
                return _fallbackSchemaOptions = SchemaOptions(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions);

            default:
                _logger?.LogWarning(
                    "The message body serialization factory ({SerializationFactory}) is not System.Text.Json-based, so the wire contract cannot be derived and messages are documented without payload schemas. " +
                    "Set AsyncApiOptions.SerializerOptions to options matching the wire format to document payload schemas.",
                    _serializationFactory.GetType());
                return null;
        }
    }

    private string Join(IReadOnlyList<MessageTypeMetadata> messages)
        => string.Join(", ", messages.Select((m) => FriendlyTypeName(Describe(m).PayloadType)).Distinct(StringComparer.Ordinal));

    /// <summary>
    /// Renders a type name for display, expanding closed generics (for example
    /// <c>Envelope&lt;OrderPlaced&gt;</c> rather than <c>Envelope`1</c>). Wire names are never
    /// derived from this; they stay faithful to the registered logical name.
    /// </summary>
    private static string FriendlyTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string name = type.Name;
        int backtickIndex = name.IndexOf('`');
        if (backtickIndex > 0)
        {
            name = name.Remove(backtickIndex);
        }

        return $"{name}<{string.Join(", ", type.GenericTypeArguments.Select(FriendlyTypeName))}>";
    }

    private static string Sanitize(string value)
    {
        // AsyncAPI object keys must match ^[A-Za-z0-9._-]+$.
        char[] result = value.ToCharArray();
        for (int i = 0; i < result.Length; i++)
        {
            char c = result[i];
            bool valid = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-';
            if (!valid)
            {
                result[i] = '_';
            }
        }

        return new string(result);
    }
}
