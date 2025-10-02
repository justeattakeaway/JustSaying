using System;
using System.Collections.Generic;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling
{
    public class MessageAttributes
    {
        private readonly Dictionary<string, MessageAttributeValue> _attributes;

        public MessageAttributes(Dictionary<string, MessageAttributeValue> attributes)
        {
            _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        }

        public MessageAttributes()
        {
            _attributes = new Dictionary<string, MessageAttributeValue>();
        }

        public MessageAttributeValue Get(string value)
        {
            return _attributes.TryGetValue(value, out MessageAttributeValue result) ? result : null;
        }

        public IReadOnlyCollection<string> GetKeys()
        {
            return _attributes.Keys;
        }
    }

}
