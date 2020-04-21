using System.Collections.Generic;

namespace JustSaying.Messaging.Channels.Context
{
    public class MessageAttributes
    {
        private readonly Dictionary<string, MessageAttributeValue> _attributes;

        public MessageAttributes(Dictionary<string, MessageAttributeValue> attributes)
        {
            _attributes = attributes;
        }

        public MessageAttributes()
        {
            _attributes = new Dictionary<string, MessageAttributeValue>();
        }

        public MessageAttributeValue Get(string value)
            => _attributes.TryGetValue(value, out var result) ? result : null;
    }
}
