using System;
using Amazon;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using NLog;

namespace JustSaying
{
    public class CreateMeABus : ICreateBus
    {
        private MessagingConfig _config;

        public CreateMeABus(MessagingConfig config)
        {
            _config = config;
        }

        public IAmJustSayingFluently ConfigurePublisherWith(Action<IPublishConfiguration> confBuilder)
        {
            confBuilder(_config);
            _config.Validate();

            var bus = new JustSayingFluently(new JustSayingBus(_config, new MessageSerialisationRegister()), new AmazonQueueCreator());
            bus.WithMonitoring(new NullOpMessageMonitor());

            return bus;
        }

        public static ICreateBus InRegion(string region)
        {
            var config = new MessagingConfig() {Region = region};

            return new CreateMeABus(config);
        }
    }
    public interface ICreateBus : IConfigurePublisher
    {

    }

    public interface IConfigurePublisher
    {
        IAmJustSayingFluently ConfigurePublisherWith(Action<IPublishConfiguration> confBuilder);
    }
}