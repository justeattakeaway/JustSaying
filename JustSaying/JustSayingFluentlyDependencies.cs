using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;

namespace JustSaying
{
    public class JustSayingFluentlyDependencies
    {
        public ILoggerFactory LoggerFactory { get; set; }
        public IMessageSubjectProvider MessageSubjectProvider { get; set; }
    }
}
