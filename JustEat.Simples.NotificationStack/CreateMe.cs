using System;
using Amazon;
using JustSaying;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Stack;
using NLog;
using MessagingConfig = JustSaying.Stack.MessagingConfig;

namespace JustSayingExtensions
{
    public static class CreateMe
    {
        private static readonly Logger Log = LogManager.GetLogger("JustSaying"); // ToDo: Dangerous!

        public static IFluentMonitoring AJustEatBus(Action<INotificationStackConfiguration> configuration)
        {
            var config = new MessagingConfig();
            configuration.Invoke(config);
            if (string.IsNullOrWhiteSpace(config.Region))
            {
                config.Region = RegionEndpoint.EUWest1.SystemName; 
                Log.Info("No Region was specified, using {0} by default.", config.Region);
            }
            config.Validate();

            return new FluentNotificationStack(new JustSayingBus(config, new MessageSerialisationRegister()), new AmazonQueueCreator());
        }
    }
}