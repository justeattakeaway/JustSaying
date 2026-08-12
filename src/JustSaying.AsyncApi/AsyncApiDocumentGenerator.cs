using System.Reflection;
using System.Text.Json;
using System.Text.Json.Schema;
using ByteBard.AsyncAPI.Models;
using JustSaying.CloudEvents;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Metadata;

namespace JustSaying.AsyncApi;

/// <summary>
/// Generates an AsyncAPI 3.0 document from the publications and subscriptions captured in an
/// <see cref="IMessagingMetadataRegistry"/>.
/// </summary>
public sealed class AsyncApiDocumentGenerator
{
    private const string CloudEventsContentType = "application/cloudevents+json";

    private readonly IMessagingMetadataRegistry _registry;
    private readonly AsyncApiOptions _options;
    private readonly IMessageBodySerializationFactory _serializationFactory;
    private readonly CloudEventOptions _cloudEventOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncApiDocumentGenerator"/> class.
    /// </summary>
    /// <param name="registry">The registry of captured publications and subscriptions.</param>
    /// <param name="options">The options configuring the generated document.</param>
    /// <param name="serializationFactory">The message body serialization factory in use, used to discover the payload wire contract.</param>
    /// <param name="cloudEventOptions">The CloudEvents options, when CloudEvents support is configured.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="registry"/> or <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public AsyncApiDocumentGenerator(
        IMessagingMetadataRegistry registry,
        AsyncApiOptions options,
        IMessageBodySerializationFactory serializationFactory = null,
        CloudEventOptions cloudEventOptions = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _serializationFactory = serializationFactory;
        _cloudEventOptions = cloudEventOptions;
    }

    /// <summary>
    /// Generates the AsyncAPI document.
    /// </summary>
    /// <returns>The generated <see cref="AsyncApiDocument"/>.</returns>
    public AsyncApiDocument Generate()
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
            DefaultContentType = _cloudEventOptions != null ? CloudEventsContentType : "application/json",
        };

        string primaryRegion = PrimaryRegion();

        AddServers(document, primaryRegion);

        var channels = new Dictionary<string, ChannelState>(StringComparer.Ordinal);
        var operationMessages = new Dictionary<string, List<MessageTypeMetadata>>(StringComparer.Ordinal);

        foreach (var publication in _registry.Publications)
        {
            if (publication.IsDynamic)
            {
                // A dynamic destination has no static address; there is no channel to document.
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

            document.Operations[operationKey] = new AsyncApiOperation()
            {
                Action = AsyncApiAction.Send,
                Channel = new AsyncApiChannelReference($"#/channels/{channel.Key}"),
                Summary = $"Publish {Join(merged)} to {publication.DestinationName}.",
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
                Messages = [.. merged.Select((m) => new AsyncApiMessageReference($"#/channels/{channel.Key}/messages/{channel.MessageKeys[WireName(m)]}"))],
            };
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

    private AsyncApiMessage CreateMessage(MessageTypeMetadata metadata)
    {
        string name = WireName(metadata);

        var message = new AsyncApiMessage()
        {
            Name = name,
            Title = metadata.MessageType.Name,
            ContentType = _cloudEventOptions != null ? CloudEventsContentType : "application/json",
        };

        var serializerOptions = ResolveSerializerOptions();
        if (serializerOptions != null)
        {
            try
            {
                var schemaNode = serializerOptions.GetJsonSchemaAsNode(metadata.MessageType, new JsonSchemaExporterOptions
                {
                    TreatNullObliviousAsNonNullable = true,
                });

                message.Payload = JsonSchemaNodeMapper.Map(schemaNode);
            }
            catch (NotSupportedException)
            {
                // The serializer cannot describe this type; the message is documented without a payload schema.
            }
        }

        return message;
    }

    private string WireName(MessageTypeMetadata metadata)
    {
        if (_cloudEventOptions != null && _cloudEventOptions.TryGetCloudEventType(metadata.MessageType, out var cloudEventType))
        {
            return cloudEventType;
        }

        return metadata.WireName ?? metadata.MessageType.Name;
    }

    private JsonSerializerOptions ResolveSerializerOptions()
    {
        var options = _options.SerializerOptions;

        if (options == null)
        {
            var factory = _serializationFactory;
            if (factory is CloudEventSerializationFactory cloudEventFactory)
            {
                factory = cloudEventFactory.DataSerializerFactory;
            }

            options = factory switch
            {
                SystemTextJsonSerializationFactory systemTextJsonFactory => systemTextJsonFactory.SerializerOptions,
                null => SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions,
                // A Newtonsoft or custom factory's wire contract cannot be derived from
                // System.Text.Json options (for example, JustSaying's Newtonsoft serializer
                // writes enums as strings), so rather than documenting a schema that may not
                // match the wire format, messages are documented without payload schemas
                // unless AsyncApiOptions.SerializerOptions is supplied explicitly.
                _ => null,
            };

            if (options == null)
            {
                return null;
            }
        }

        if (options.TypeInfoResolver == null)
        {
            // The schema exporter requires a resolver to be set explicitly; the serializers rely on
            // it being applied lazily. Under Native AOT there is no reflection resolver to fall back
            // to, so messages are documented without payload schemas.
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                return null;
            }

#pragma warning disable IL2026, IL3050
            options = new JsonSerializerOptions(options)
            {
                TypeInfoResolver = JsonSerializerOptions.Default.TypeInfoResolver,
            };
#pragma warning restore IL2026, IL3050
        }

        return options;
    }

    private string Join(IReadOnlyList<MessageTypeMetadata> messages)
        => string.Join(", ", messages.Select((m) => m.MessageType.Name));

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
