using System;
using System.Collections.Generic;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using JustSaying.Naming;

namespace JustSaying
{
    public class MessagingConfig : IMessagingConfig
    {
        public MessagingConfig()
        {
            PublishFailureReAttempts = JustSayingConstants.DefaultPublisherRetryCount;
            PublishFailureBackoff = JustSayingConstants.DefaultPublisherRetryInterval;
            AdditionalSubscriberAccounts = new List<string>();
            MessageSubjectProvider = new NonGenericMessageSubjectProvider();
            TopicNamingConvention = new DefaultNamingConventions();
            QueueNamingConvention = new DefaultNamingConventions();
        }

        public int PublishFailureReAttempts { get; set; }
        public TimeSpan PublishFailureBackoff { get; set; }
        public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
        public IReadOnlyCollection<string> AdditionalSubscriberAccounts { get; set; }
        public string Region { get; set; }
        public IMessageSubjectProvider MessageSubjectProvider { get; set; }
        public ITopicNamingConvention TopicNamingConvention { get; set; }
        public IQueueNamingConvention QueueNamingConvention { get; set; }

        public virtual void Validate()
        {
            if (MessageSubjectProvider == null)
            {
                throw new InvalidOperationException($"Config cannot have a null for the {nameof(MessageSubjectProvider)} property.");
            }
        }
    }
}
