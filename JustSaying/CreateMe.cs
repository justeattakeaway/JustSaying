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
    public static class CreateMe
    {
        public static IAmJustSayingFluently ABus(Action<IPublishConfiguration> configuration)
        {
            var config = new MessagingConfig();
            configuration.Invoke(config);
            config.Validate();

            return new JustSayingFluently(new JustSayingBus(config, new MessageSerialisationRegister()), new AmazonQueueCreator());
        }
    }
}