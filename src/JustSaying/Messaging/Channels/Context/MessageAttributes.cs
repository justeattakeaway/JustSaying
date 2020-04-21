using System.Collections.Generic;

namespace JustSaying.Messaging.Channels.Context
{
    public class MessageAttributes
    {
        private readonly Dictionary<string, Amazon.SQS.Model.MessageAttributeValue> _attributes;

        public MessageAttributes(Dictionary<string, Amazon.SQS.Model.MessageAttributeValue> attributes)
        {
            _attributes = attributes;
        }

        public MessageAttributes()
        {
            _attributes = new Dictionary<string, Amazon.SQS.Model.MessageAttributeValue>();
        }

        public Amazon.SQS.Model.MessageAttributeValue Get(string value)
            => _attributes.TryGetValue(value, out var result) ? result : null;
    }
}
