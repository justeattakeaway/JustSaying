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
/// Generates an AsyncAPI 3.1 document from the publications and subscriptions captured in an
/// <see cref="IMessagingMetadataRegistry"/>.
/// </summary>
public sealed class AsyncApiDocumentGenerator
{
    private const string JsonContentType = "application/json";

    private readonly IMessagingMetadataRegistry _registry;
    private readonly AsyncApiOptions _options;
    private readonly ILogger<AsyncApiDocumentGenerator> _logger;
    private readonly object _syncRoot = new();

    // Per-generation state: each registration is described once, and the serializer-derived
    // schema options and non-System.Text.Json warnings are computed once per serializer.
    private readonly Dictionary<MessageTypeMetadata, MessageDescription> _descriptions = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<JsonSerializerOptions, JsonSerializerOptions> _schemaOptions = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Type> _undescribableFormats = [];
    private bool _unknownFormatReported;

    // The component schemas of recursive payload types, keyed by type and the options that shape it.
    private readonly Dictionary<(Type Type, JsonSerializerOptions Options), string> _componentKeys = [];
    private readonly Dictionary<string, AsyncApiJsonSchema> _componentSchemas = new(StringComparer.Ordinal);

    /// <summary>
    /// The <see cref="AppContext"/> data name under which JustSaying.AsyncApi.GetDocument (the
    /// build-time generation tool) registers an <see cref="Action{T1, T2}"/> that receives each
    /// warning's code and message, to report them as build warnings. The tool references nothing
    /// from this assembly, so the name is the contract.
    /// </summary>
    internal const string BuildWarningsDataName = "JustSaying.AsyncApi.GetDocument.ReportWarning";

    // Each warning has a stable code (JSAA1xx; the tool's own errors are JSAA0xx), which is its log
    // event id and, at build time, its MSBuild warning code, so that it can be suppressed by code.
    private static readonly EventId EmptyDocument = new(101, nameof(EmptyDocument));
    private static readonly EventId DynamicPublicationOmitted = new(102, nameof(DynamicPublicationOmitted));
    private static readonly EventId PayloadSchemaUnavailable = new(103, nameof(PayloadSchemaUnavailable));
    private static readonly EventId NoTypeInfoResolver = new(104, nameof(NoTypeInfoResolver));
    private static readonly EventId FormatNotDescribable = new(105, nameof(FormatNotDescribable));
    private static readonly EventId FormatUnknown = new(106, nameof(FormatUnknown));
    private static readonly EventId DuplicateMessageName = new(107, nameof(DuplicateMessageName));

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
    /// <param name="logger">The logger used to surface why parts of the document are omitted.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="registry"/> or <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public AsyncApiDocumentGenerator(
        IMessagingMetadataRegistry registry,
        AsyncApiOptions options,
        ILogger<AsyncApiDocumentGenerator> logger = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <summary>
    /// Gets the name of the application the document describes, used as the title when
    /// <see cref="AsyncApiOptions.Title"/> is not set. This is the host's application name, which
    /// is the application's own name at build time too, where the entry assembly is the generation tool.
    /// </summary>
    internal string ApplicationName { get; init; }

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
                Title = _options.Title ?? ApplicationName ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "JustSaying application",
                Version = _options.Version,
                Description = _options.Description,
            },
            DefaultContentType = JsonContentType,
        };

        string primaryRegion = PrimaryRegion();

        AddServers(document, primaryRegion);

        if (_registry.Publications.Count == 0 && _registry.Subscriptions.Count == 0)
        {
            Warn(
                EmptyDocument,
                "The generated AsyncAPI document is empty: no publications or subscriptions were captured. " +
                "If the application does configure messaging, ensure AddJustSaying ran in the same service collection before the document was generated.");
        }

        var channels = new Dictionary<string, ChannelState>(StringComparer.Ordinal);
        var operationMessages = new Dictionary<string, List<MessageTypeMetadata>>(StringComparer.Ordinal);
        var envelopedMessages = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var publication in _registry.Publications)
        {
            if (publication.IsDynamic)
            {
                // A dynamic destination has no static address; there is no channel to document.
                Warn(
                    DynamicPublicationOmitted,
                    $"Publication of {Join(publication.Messages)} uses a dynamic destination name computed per message, so it has no static address and is omitted from the AsyncAPI document.");
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
                if (!envelopedMessages.TryGetValue(operationKey, out var enveloped))
                {
                    envelopedMessages[operationKey] = enveloped = new HashSet<string>(StringComparer.Ordinal);
                }

                foreach (var message in publication.Messages)
                {
                    enveloped.Add(WireName(message));
                }
            }

            document.Operations[operationKey] = new AsyncApiOperation()
            {
                Action = AsyncApiAction.Send,
                Channel = new AsyncApiChannelReference($"#/channels/{channel.Key}"),
                Summary = $"Publish {Join(merged)} to {publication.DestinationName}.",
                Description = QueueEnvelopeDescription(merged, envelopedMessages.GetValueOrDefault(operationKey)),
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

        if (_componentSchemas.Count > 0)
        {
            document.Components ??= new AsyncApiComponents();
            foreach (var component in _componentSchemas.OrderBy((c) => c.Key, StringComparer.Ordinal))
            {
                document.Components.Schemas[component.Key] = component.Value;
            }
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

        /// <summary>
        /// Gets the registration each documented message wire name was described from.
        /// </summary>
        public Dictionary<string, MessageTypeMetadata> MessageSources { get; } = new(StringComparer.Ordinal);
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
                state.MessageSources[wireName] = message;
                state.Channel.Messages[messageKey] = CreateMessage(message);
            }
            else if (state.MessageSources[wireName] is var documented && !Describe(documented).IsSameMessageAs(Describe(message)))
            {
                // A reader identifies a message by its name, so two different messages under one name
                // on one destination can't be told apart. The first registered is documented, which
                // keeps the channel consistent with the operations, and the conflict is reported.
                Warn(
                    DuplicateMessageName,
                    $"{FriendlyTypeName(Describe(message).PayloadType)} and {FriendlyTypeName(Describe(documented).PayloadType)} are both identified as '{wireName}' on {address}, " +
                    $"so consumers cannot tell them apart; only {FriendlyTypeName(Describe(documented).PayloadType)} is documented. Give each message on a destination a distinct name.");
            }
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

    private const string QueueEnvelopeShape = "{ \"Message\": \"...\", \"Subject\": \"...\" }";

    /// <summary>
    /// Describes how documented payloads are actually written to the queue. Without raw messages,
    /// JustSaying wraps the payload in its own queue envelope, so the SQS body is not the
    /// documented payload itself. A queue can carry a mix — for example a legacy message alongside
    /// a self-describing CloudEvent, which is never wrapped — so the envelope is described per
    /// message rather than for the operation as a whole.
    /// </summary>
    private string QueueEnvelopeDescription(IReadOnlyList<MessageTypeMetadata> messages, HashSet<string> envelopedWireNames)
    {
        if (envelopedWireNames == null || envelopedWireNames.Count == 0)
        {
            return null;
        }

        var enveloped = messages.Where((m) => envelopedWireNames.Contains(WireName(m))).ToList();
        var verbatim = messages.Where((m) => !envelopedWireNames.Contains(WireName(m))).ToList();

        string subject = verbatim.Count == 0 ? "The publisher wraps each message" : $"The publisher wraps {Join(enveloped)}";
        string description =
            $"{subject} in JustSaying's queue envelope: the SQS message body is {QueueEnvelopeShape}, " +
            "and the documented message payload is the JSON-encoded string in its \"Message\" property. " +
            "Publish with raw messages to send the payload verbatim instead.";

        if (verbatim.Count > 0)
        {
            description += $" {Join(verbatim)} {(verbatim.Count == 1 ? "is" : "are")} sent verbatim: the SQS message body is the documented message payload.";
        }

        return description;
    }

    /// <summary>
    /// Describes how documented payloads actually arrive on the queue. Without raw message
    /// delivery, SNS wraps each message in its notification envelope, so the SQS body is not
    /// the documented payload itself. A point-to-point subscription that does not use raw message
    /// delivery accepts either the bare payload or JustSaying's queue envelope, since the producer
    /// decides which it sends.
    /// </summary>
    private static string DeliveryDescription(SubscriptionMetadata subscription)
    {
        if (subscription.TopicName == null)
        {
            return subscription.RawMessageDelivery
                ? "The queue subscription uses raw message delivery: the SQS message body is the documented message payload."
                : $"The queue subscription accepts the SQS message body either as the documented message payload or wrapped in JustSaying's queue envelope, {QueueEnvelopeShape}, with the documented message payload as the JSON-encoded string in its \"Message\" property.";
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
    private sealed record MessageDescription(string WireName, Type PayloadType, string ContentType, AsyncApiJsonSchema Payload)
    {
        /// <summary>
        /// Whether two registrations describe the same message, as a publication and a subscription
        /// of one type on one queue do.
        /// </summary>
        public bool IsSameMessageAs(MessageDescription other)
            => PayloadType == other.PayloadType && string.Equals(ContentType, other.ContentType, StringComparison.Ordinal);
    }

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

        var body = DescribeBody(metadata.MessageType, metadata.BodyFormat);

        // A CloudEvent is identified by its `type`; anything else by its registered logical name.
        string wireName = (metadata.BodyFormat as ICloudEventMessageBodySerializer)?.Type
            ?? metadata.WireName
            ?? metadata.MessageType.Name;

        description = new MessageDescription(wireName, body.PayloadType, body.ContentType, body.Payload);
        _descriptions[metadata] = description;
        return description;
    }

    private (Type PayloadType, string ContentType, AsyncApiJsonSchema Payload) DescribeBody(Type messageType, IMessageBodyFormat format)
    {
        switch (format)
        {
            case ICloudEventMessageBodySerializer cloudEvent:
                // The wire format is the CloudEvents structured-mode envelope with the payload under
                // "data"; documenting the bare payload schema would hand consumers the wrong shape.
                // Whether the handler sees the envelope or just the data is the same on the wire.
                var data = DescribeBody(cloudEvent.DataType, cloudEvent.DataFormat);
                return (cloudEvent.DataType, cloudEvent.ContentType, CreateCloudEventEnvelopeSchema(cloudEvent, data.Payload));

            case ISystemTextJsonMessageBodySerializer systemTextJson:
                return (messageType, systemTextJson.ContentType ?? JsonContentType, ExportPayloadSchema(messageType, SchemaOptions(systemTextJson.SerializerOptions)));

            default:
                return (messageType, format?.ContentType ?? JsonContentType, ExportPayloadSchema(messageType, UndescribableFormatSchemaOptions(format)));
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
            var mapper = new JsonSchemaNodeMapper(serializerOptions, (type, createSchema) => AddComponentSchema(type, serializerOptions, createSchema));
            var schemaNode = serializerOptions.GetJsonSchemaAsNode(messageType, mapper.ExporterOptions);

            return mapper.Map(schemaNode);
        }
        catch (NotSupportedException exception)
        {
            // The serializer cannot describe this type; the message is documented without a payload schema.
            Warn(
                PayloadSchemaUnavailable,
                $"A payload schema for message type {FriendlyTypeName(messageType)} could not be derived ({exception.Message}); the message is documented without one.");
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
                Warn(
                    NoTypeInfoResolver,
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
    private JsonSerializerOptions UndescribableFormatSchemaOptions(IMessageBodyFormat format)
    {
        if (_options.SerializerOptions != null)
        {
            return SchemaOptions(_options.SerializerOptions);
        }

        if (format == null)
        {
            if (!_unknownFormatReported)
            {
                _unknownFormatReported = true;
                Warn(
                    FormatUnknown,
                    $"A message body serializer does not describe its format (it does not implement {nameof(IMessageBodyFormat)}), so its messages are documented as JSON without payload schemas. " +
                    "Implement ISystemTextJsonMessageBodySerializer on a System.Text.Json-based serializer, or set AsyncApiOptions.SerializerOptions to options matching the wire format, to document payload schemas.");
            }
        }
        else if (_undescribableFormats.Add(format.GetType()))
        {
            Warn(
                FormatNotDescribable,
                $"The message body serializer ({FriendlyTypeName(format.GetType())}) is not System.Text.Json-based, so the wire contract cannot be derived and its messages are documented without payload schemas. " +
                "Set AsyncApiOptions.SerializerOptions to options matching the wire format to document payload schemas.");
        }

        return null;
    }

    private void Warn(EventId eventId, string message)
    {
        _logger?.Log(LogLevel.Warning, eventId, message, null, static (state, _) => state);

        if (AppContext.GetData(BuildWarningsDataName) is Action<string, string> reportBuildWarning)
        {
            reportBuildWarning($"JSAA{eventId.Id}", message);
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

    /// <summary>
    /// Adds the schema of a recursive payload type to the document's component schemas, unless it
    /// is already there, and returns the reference to it. The same type can be described differently
    /// by different serializer options, so each pairing gets a component of its own; its key is the
    /// type's name, made unique with a deterministic suffix.
    /// </summary>
    private string AddComponentSchema(Type type, JsonSerializerOptions serializerOptions, Func<AsyncApiJsonSchema> createSchema)
    {
        if (!_componentKeys.TryGetValue((type, serializerOptions), out var key))
        {
            key = AllocateKey([Sanitize(FriendlyTypeName(type))], (candidate) => !_componentSchemas.ContainsKey(candidate));

            // The key is claimed before the schema is created, so that the type's references to
            // itself resolve to it.
            _componentKeys[(type, serializerOptions)] = key;
            _componentSchemas[key] = null;
            _componentSchemas[key] = createSchema();
        }

        return $"#/components/schemas/{key}";
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
