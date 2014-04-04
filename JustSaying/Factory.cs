using System;
using Amazon;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using NLog;

namespace JustSaying
{
    /// <summary>
    /// Factory providing a messaging bus
    /// </summary>
    public static class Factory
    {
        private static readonly Logger Log = LogManager.GetLogger("JustSaying"); // ToDo: Dangerous!

        public static IFluentMonitoring JustSaying(Action<IPublishConfiguration> configuration)
        {
            var config = new MessagingConfig();
            configuration.Invoke(config);
            config.Validate();

            if (string.IsNullOrWhiteSpace(config.Region))
            {
                config.Region = RegionEndpoint.EUWest1.SystemName; // ToDo: Why is this in the base impl?
                Log.Info("No Region was specified, using {0} by default.", config.Region);
            }

            return new JustSayingFluently(new JustSayingBus(config, new MessageSerialisationRegister()), new AmazonQueueCreator());
        }
    }
}