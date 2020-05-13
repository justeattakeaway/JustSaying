using JustSaying.Messaging.MessageSerialization;
using JustSaying.Naming;
using Microsoft.Extensions.Logging;

namespace JustSaying
{
    public class JustSayingFluentlyDependencies
    {
        public ILoggerFactory LoggerFactory { get; set; }
        public IMessageSubjectProvider MessageSubjectProvider { get; set; }
        public IQueueNamingConvention QueueNamingConvention { get; set; }
        public ITopicNamingConvention TopicNamingConvention { get; set; }
        public IMessageSerializationFactory SerializationFactory { get; set; }
    }
}
