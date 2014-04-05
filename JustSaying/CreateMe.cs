using System;
using Amazon;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using NLog;

namespace JustSaying
{
    /// <summary>
    /// Factory providing a messaging bus
    /// </summary>
    public static class CreateMe
    {
        private static readonly Logger Log = LogManager.GetLogger("JustSaying"); // ToDo: Dangerous!

        public static IAmJustSayingFluently ABus(Action<IPublishConfiguration> configuration)
        {
            var config = new MessagingConfig();
            configuration.Invoke(config);
            config.Validate();

            if (string.IsNullOrWhiteSpace(config.Region))
            {
                config.Region = RegionEndpoint.EUWest1.SystemName; // ToDo: Why is this in the base impl rather than JE? Config validation does it for us no?
                Log.Info("No Region was specified, using {0} by default.", config.Region);
            }

            var bus = new JustSayingFluently(new JustSayingBus(config, new MessageSerialisationRegister()), new AmazonQueueCreator());
            bus.WithMonitoring(new NullOpMessageMonitor());

            return bus;
        }
    }
}