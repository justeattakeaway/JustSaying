using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.Messaging;

/// <summary>
/// Selects the <see cref="IMessageBodySerializer"/> to use for an inbound message.
/// </summary>
internal interface IInboundMessageSerializerResolver
{
    IMessageBodySerializer Resolve(string body, string subject, MessageAttributes attributes);
}

/// <summary>
/// Always returns the same serializer — the single-type subscription case.
/// </summary>
internal sealed class SingleInboundMessageSerializerResolver(IMessageBodySerializer serializer) : IInboundMessageSerializerResolver
{
    private readonly IMessageBodySerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public IMessageBodySerializer Resolve(string body, string subject, MessageAttributes attributes) => _serializer;
}

/// <summary>
/// Resolves the serializer by discriminating the message's logical type name (via an ordered chain of
/// <see cref="IMessageTypeDiscriminator"/>) and looking it up in a name → serializer map — the
/// multi-type subscription case.
/// </summary>
internal sealed class DiscriminatingInboundMessageSerializerResolver : IInboundMessageSerializerResolver
{
    private readonly IReadOnlyList<IMessageTypeDiscriminator> _discriminators;
    private readonly IReadOnlyDictionary<string, IMessageBodySerializer> _serializersByName;

    public DiscriminatingInboundMessageSerializerResolver(
        IReadOnlyList<IMessageTypeDiscriminator> discriminators,
        IReadOnlyDictionary<string, IMessageBodySerializer> serializersByName)
    {
        _discriminators = discriminators ?? throw new ArgumentNullException(nameof(discriminators));
        _serializersByName = serializersByName ?? throw new ArgumentNullException(nameof(serializersByName));
    }

    public IMessageBodySerializer Resolve(string body, string subject, MessageAttributes attributes)
    {
        var context = new MessageDiscriminationContext(body, subject, attributes);

        foreach (var discriminator in _discriminators)
        {
            if (discriminator.TryGetMessageTypeName(context, out var typeName) && !string.IsNullOrEmpty(typeName))
            {
                // The first discriminator to recognise the message decides its type. Falling through to
                // a later one when that type isn't registered would let a weaker signal (such as the SNS
                // Subject on a CloudEvent of an unknown `type`) deserialize the message as the wrong type.
                if (_serializersByName.TryGetValue(typeName, out var serializer))
                {
                    return serializer;
                }

                break;
            }
        }

        // Not a format problem but a routing one (an unknown type, or a type the producer shipped before
        // this consumer), so the message must not be deleted: the redrive policy moves it to the error queue.
        throw new UnroutableMessageException(
            $"No message type registered on this queue matches the message ({DescribeDiscriminatedNames(context)}). " +
            $"Registered types: {string.Join(", ", _serializersByName.Keys.Select(name => $"'{name}'"))}.");
    }

    private string DescribeDiscriminatedNames(MessageDiscriminationContext context)
    {
        // Only runs for a message that failed to route, so asking each discriminator again is fine.
        return string.Join(", ", _discriminators.Select(discriminator =>
            discriminator.TryGetMessageTypeName(context, out var typeName) && !string.IsNullOrEmpty(typeName)
                ? $"{discriminator} '{typeName}'"
                : $"{discriminator}: none"));
    }
}
