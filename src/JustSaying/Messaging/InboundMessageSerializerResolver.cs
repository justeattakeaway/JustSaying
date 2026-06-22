using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

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
    private readonly IMessageBodySerializer _serializer = serializer;

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
            if (discriminator.TryGetMessageTypeName(context, out var typeName)
                && !string.IsNullOrEmpty(typeName)
                && _serializersByName.TryGetValue(typeName, out var serializer))
            {
                return serializer;
            }
        }

        throw new MessageFormatNotSupportedException(
            $"Could not resolve a registered message type for the inbound message (subject: '{subject ?? "<none>"}'). " +
            "Ensure the message's subject or type matches a type registered on this multi-type queue subscription.");
    }
}
