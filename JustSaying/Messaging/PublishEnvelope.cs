using System;
using System.Collections.Generic;
using JustSaying.Models;

namespace JustSaying.Messaging
{
    public class PublishEnvelope
    {
        public PublishEnvelope(Message message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public Message Message { get; }

        public TimeSpan? Delay { get; set; }

        public IDictionary<string, MessageAttributeValue> MessageAttributes { get; set; }

        public PublishEnvelope AddMessageAttribute(string key, MessageAttributeValue value)
        {
            if (MessageAttributes == null)
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>();
            }

            MessageAttributes.Add(key, value);

            return this;
        }

        public PublishEnvelope AddMessageAttribute(string key, string value)
        {
            return AddMessageAttribute(key, new MessageAttributeValue
            {
                StringValue = value,
                DataType = "String"
            });
        }
    }
}
