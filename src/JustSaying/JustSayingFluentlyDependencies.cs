using JustSaying.Messaging.MessageSerialization;
using JustSaying.Naming;
using Microsoft.Extensions.Logging;

namespace JustSaying
{
    public class JustSayingFluentlyDependencies
    {
        public ILoggerFactory LoggerFactory { get; set; }
        public IMessageSubjectProvider MessageSubjectProvider { get; set; }
        public IDefaultQueueNamingConvention QueueNamingConvention { get; set; }
        public IDefaultTopicNamingConvention TopicNamingConvention { get; set; }
    }
}
