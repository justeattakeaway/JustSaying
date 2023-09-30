using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.Messaging.MessageHandling;

/// <summary>
/// A collection of <see cref="MessageAttributeValue"/> values returned by <see cref="IMessageSerializationRegister"/>.
/// </summary>
public class MessageAttributes(Dictionary<string, MessageAttributeValue> attributes)
{
    private readonly Dictionary<string, MessageAttributeValue> _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));

    public MessageAttributes()
        : this(new Dictionary<string, MessageAttributeValue>())
    {
    }

    public MessageAttributeValue Get(string value)
        => _attributes.TryGetValue(value, out MessageAttributeValue result) ? result : null;
}
