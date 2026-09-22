using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.CloudEvents;

/// <summary>
/// An <see cref="IMessageBodySerializationFactory"/> that produces
/// <see cref="CloudEventMessageBodySerializer{TMessage}"/> instances, wrapping the serializers from an
/// inner factory (used for the CloudEvents <c>data</c> payload).
/// </summary>
public sealed class CloudEventSerializationFactory : IMessageBodySerializationFactory
{
    private readonly IMessageBodySerializationFactory _dataSerializerFactory;
    private readonly IMessageMetadataProvider _metadataProvider;
    private readonly CloudEventOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudEventSerializationFactory"/> class.
    /// </summary>
    /// <param name="dataSerializerFactory">The factory whose serializers handle the <c>data</c> payload.</param>
    /// <param name="metadataProvider">Provides the CloudEvents <c>id</c> and <c>time</c> from messages.</param>
    /// <param name="options">The CloudEvents options (source, content type and per-type <c>type</c> mappings).</param>
    public CloudEventSerializationFactory(
        IMessageBodySerializationFactory dataSerializerFactory,
        IMessageMetadataProvider metadataProvider,
        CloudEventOptions options)
    {
        _dataSerializerFactory = dataSerializerFactory ?? throw new ArgumentNullException(nameof(dataSerializerFactory));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (_options.Source is null)
        {
            throw new ArgumentException($"{nameof(CloudEventOptions)}.{nameof(CloudEventOptions.Source)} must be set.", nameof(options));
        }
    }

    /// <inheritdoc />
    public IMessageBodySerializer<TMessage> GetSerializer<TMessage>() where TMessage : class
    {
        if (!_options.TryGetCloudEventType(typeof(TMessage), out var type))
        {
            throw new InvalidOperationException(
                $"No CloudEvents 'type' is configured for message type '{typeof(TMessage).FullName}'. " +
                $"Configure one via {nameof(CloudEventOptions)}.{nameof(CloudEventOptions.WithCloudEventType)}<{typeof(TMessage).Name}>(\"...\").");
        }

        var dataSerializer = _dataSerializerFactory.GetSerializer<TMessage>();
        return new CloudEventMessageBodySerializer<TMessage>(dataSerializer, _metadataProvider, _options.Source, type, _options.DataContentType);
    }
}
