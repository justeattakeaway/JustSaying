using System;
using System.Collections.Generic;

namespace JustSaying.Messaging
{
    public class PublishMetadata
    {
        public TimeSpan? Delay { get; set; }

        public IDictionary<string, MessageAttributeValue> MessageAttributes { get; set; }

        public PublishMetadata AddMessageAttribute(string key, MessageAttributeValue value)
        {
            if (MessageAttributes == null)
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>(StringComparer.Ordinal);
            }

            MessageAttributes[key] = value;

            return this;
        }

        public PublishMetadata AddMessageAttribute(string key, string value)
        {
            return AddMessageAttribute(key, new MessageAttributeValue
            {
                StringValue = value,
                DataType = "String"
            });
        }
    }
}
