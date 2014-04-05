using System;
using JustSaying;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Stack;
using MessagingConfig = JustSaying.Stack.MessagingConfig;

namespace JustSayingExtensions
{
    public static class CreateMe
    {
        public static IFluentMonitoring AJustEatBus(Action<INotificationStackConfiguration> configuration)
        {
            var config = new MessagingConfig();
            configuration.Invoke(config);

            config.Validate();

            return new FluentNotificationStack(new JustSayingBus(config, new MessageSerialisationRegister()), new AmazonQueueCreator());
        }
    }
}