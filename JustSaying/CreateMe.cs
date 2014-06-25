using System;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;

namespace JustSaying
{
    public class CreateMeABus
    {
        private readonly MessagingConfig _config;

        public CreateMeABus(MessagingConfig config)
        {
            _config = config;
        }

        public static IMayWantOptionalSettings InRegion(string region)
        {
            var config = new MessagingConfig {Region = region};

            config.Validate();

            var bus = new JustSayingFluently(new JustSayingBus(config, new MessageSerialisationRegister()), new AmazonQueueCreator());
            bus.WithMonitoring(new NullOpMessageMonitor());

            return bus;
        }
    }
}