using System;
using System.Collections.Generic;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Naming;

namespace JustSaying
{
    public interface IMessagingConfig : IPublishConfiguration
    {
        IList<string> Regions { get; }
        Func<string> GetActiveRegion { get; set; }
        IMessageSubjectProvider MessageSubjectProvider { get; set; }
        ITopicNamingConvention TopicNamingConvention { get; set; }
        IQueueNamingConvention QueueNamingConvention { get; set; }
        IConsumerConfig ConsumerConfig { get; set; }
        void Validate();
    }
}
