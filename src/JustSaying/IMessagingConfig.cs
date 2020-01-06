using System;
using System.Collections.Generic;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Naming;

namespace JustSaying
{
    public interface IMessagingConfig : IPublishConfiguration //ToDo: This vs publish config. Clean it up. not good.
    {
        IList<string> Regions { get; }
        Func<string> GetActiveRegion { get; set; }
        IMessageSubjectProvider MessageSubjectProvider { get; set; }
        IDefaultTopicNamingConvention DefaultTopicNamingConvention { get; set; }
        IDefaultQueueNamingConvention DefaultQueueNamingConvention { get; set; }
        void Validate();
    }
}
