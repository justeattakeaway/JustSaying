using System;
using System.Collections.Generic;

namespace JustSaying.Messaging.Channels.Context
{
    /// <summary>
    /// A collection of <see cref="MessageAttributeValue"/> returned by <see cref="IMessageSerializationRegister"/>.
    /// </summary>
    public class MessageAttributes
    {
        private readonly IDictionary<string, MessageAttributeValue> _attributes;

        public MessageAttributes(Dictionary<string, MessageAttributeValue> attributes)
        {
            _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        }

        public MessageAttributes()
            : this(new Dictionary<string, MessageAttributeValue>())
        {
        }

        public MessageAttributeValue Get(string value)
            => _attributes.TryGetValue(value, out MessageAttributeValue result) ? result : null;
    }
}
