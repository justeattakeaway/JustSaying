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

        AddServers(document);

        foreach (var publication in _registry.Publications)
        {
            if (publication.IsDynamic)
            {
                // A dynamic destination has no static address; there is no channel to document.
                continue;
            }

            var channelKey = AddChannel(
                document,
                publication.DestinationName,
                publication.DestinationKind,
                $"The {publication.DestinationName} {(publication.DestinationKind == MessagingDestinationKind.SnsTopic ? "SNS topic" : "SQS queue")}.",
                publication.Messages);

            document.Operations[Sanitize($"send-{channelKey}")] = new AsyncApiOperation()
            {
                Action = AsyncApiAction.Send,
                Channel = new AsyncApiChannelReference($"#/channels/{channelKey}"),
                Summary = $"Publish {Join(publication.Messages)} to {publication.DestinationName}.",
                Messages = [.. publication.Messages.Select((m) => new AsyncApiMessageReference($"#/channels/{channelKey}/messages/{MessageKey(m)}"))],
            };
        }

        foreach (var subscription in _registry.Subscriptions)
        {
            var description = subscription.TopicName != null
                ? $"The {subscription.QueueName} SQS queue, subscribed to the {subscription.TopicName} SNS topic."
                : $"The {subscription.QueueName} SQS queue.";

            var channelKey = AddChannel(
                document,
                subscription.QueueName,
                MessagingDestinationKind.SqsQueue,
                description,
                subscription.Messages);

            document.Operations[Sanitize($"receive-{channelKey}")] = new AsyncApiOperation()
            {
                Action = AsyncApiAction.Receive,
                Channel = new AsyncApiChannelReference($"#/channels/{channelKey}"),
                Summary = $"Receive {Join(subscription.Messages)} from {subscription.QueueName}.",
                Messages = [.. subscription.Messages.Select((m) => new AsyncApiMessageReference($"#/channels/{channelKey}/messages/{MessageKey(m)}"))],
            };
        }

        _options.PostProcess?.Invoke(document);

        return document;
    }

    private void AddServers(AsyncApiDocument document)
    {
        var region = _registry.Region;
        if (region == null)
        {
            return;
        }

        bool anyTopics = _registry.Publications.Any((p) => p.DestinationKind == MessagingDestinationKind.SnsTopic)
            || _registry.Subscriptions.Any((s) => s.TopicName != null);
        bool anyQueues = _registry.Publications.Any((p) => p.DestinationKind == MessagingDestinationKind.SqsQueue)
            || _registry.Subscriptions.Count > 0;

        if (anyTopics)
        {
            document.Servers["sns"] = new AsyncApiServer()
            {
                Host = $"sns.{region}.amazonaws.com",
                Protocol = "sns",
                Description = $"Amazon SNS in {region}.",
            };
        }

        if (anyQueues)
        {
            document.Servers["sqs"] = new AsyncApiServer()
            {
                Host = $"sqs.{region}.amazonaws.com",
                Protocol = "sqs",
                Description = $"Amazon SQS in {region}.",
            };
        }
    }

    private string AddChannel(
        AsyncApiDocument document,
        string address,
        MessagingDestinationKind kind,
        string description,
        IReadOnlyList<MessageTypeMetadata> messages)
    {
        string channelKey = Sanitize(address);
        if (document.Channels.TryGetValue(channelKey, out var existing) && existing.Address != address)
        {
            // A topic and a queue can share a name; keep the channel keys distinct.
            channelKey = Sanitize($"{address}-{(kind == MessagingDestinationKind.SnsTopic ? "topic" : "queue")}");
        }

        if (!document.Channels.TryGetValue(channelKey, out var channel))
        {
            channel = new AsyncApiChannel()
            {
                Address = address,
                Description = description,
            };

            string serverKey = kind == MessagingDestinationKind.SnsTopic ? "sns" : "sqs";
            if (document.Servers.ContainsKey(serverKey))
            {
                channel.Servers.Add(new AsyncApiServerReference($"#/servers/{serverKey}"));
            }

            document.Channels[channelKey] = channel;
        }

        foreach (var message in messages)
        {
            channel.Messages[MessageKey(message)] = CreateMessage(message);
        }

        return channelKey;
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

            options = factory is SystemTextJsonSerializationFactory systemTextJsonFactory
                ? systemTextJsonFactory.SerializerOptions
                : SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions;
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

    private string MessageKey(MessageTypeMetadata metadata) => Sanitize(WireName(metadata));

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
