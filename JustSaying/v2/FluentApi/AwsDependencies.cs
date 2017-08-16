using JustSaying.AwsTools;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.v2.FluentApi
{
    public interface IAwsDependencies
    {
        IAwsClientFactory AwsClientFactory { get; set; }
        IAwsNamingStrategy NamingStrategy { get; set; }
        IMessageSerialisationRegister SerialisationRegister { get; set; }
        IMessageSerialisationFactory SerialisationFactory { get; set; }
        IMessageMonitor MessageMonitor { get; set; }
        ILoggerFactory LoggerFactory { get; set; }
    }

    public class AwsDependencies : IAwsDependencies
    {
        public IAwsClientFactory AwsClientFactory { get; set; }
        public IAwsNamingStrategy NamingStrategy { get; set; }
        public IMessageSerialisationRegister SerialisationRegister { get; set; }
        public IMessageSerialisationFactory SerialisationFactory { get; set; }
        public IMessageMonitor MessageMonitor { get; set; }
        public ILoggerFactory LoggerFactory { get; set; }
    }
}