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

        public int? DelaySeconds { get; set; }

        public IDictionary<string, MessageAttributeValue> MessageAttributes { get; } = new Dictionary<string, MessageAttributeValue>();
    }
}
