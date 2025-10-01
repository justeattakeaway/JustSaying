using System;
using System.Collections.Generic;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling;

public class MessageAttributes(Dictionary<string, MessageAttributeValue> attributes)
{
    private readonly Dictionary<string, MessageAttributeValue> _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));

    public MessageAttributes() : this(new Dictionary<string, MessageAttributeValue>())
    {
    }

    public MessageAttributeValue Get(string value) => _attributes.TryGetValue(value, out MessageAttributeValue result) ? result : null;

    public IReadOnlyCollection<string> GetKeys() => _attributes.Keys;
}
