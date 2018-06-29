using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;

namespace JustSaying
{
    public class JustSayingFluentlyLogging
    {
        public ILoggerFactory LoggerFactory { get; set; }
        public IMessageSubjectProvider MessageSubjectProvider { get; set; }
    }
}
