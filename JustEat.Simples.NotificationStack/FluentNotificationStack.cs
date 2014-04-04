using System;
using Amazon;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.Messages;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying;
using JustSaying.Lookups;
using IPublishEndpointProvider = JustSaying.Lookups.IPublishEndpointProvider;
using SnsPublishEndpointProvider = JustSaying.Stack.Lookups.SnsPublishEndpointProvider;
using SqsSubscribtionEndpointProvider = JustSaying.Stack.Lookups.SqsSubscribtionEndpointProvider;

namespace JustSaying.Stack
{
    /// <summary>
    /// This is not the perfect shining example of a fluent API YET!
    /// Intended usage:
    /// 1. Call Register()
    /// 2. Set subscribers - WithSqsTopicSubscriber() / WithSnsTopicSubscriber() etc
    /// 3. Set Handlers - WithTopicMessageHandler()
    /// </summary>
    public class FluentNotificationStack : FluentMessagingMule
    {
        private FluentNotificationStack(INotificationStack stack, IVerifyAmazonQueues queueCreator): base(stack, queueCreator)
        {
        }

        public static IFluentMonitoring Register(Action<INotificationStackConfiguration> configuration)
        {
            var config = new MessagingConfig();
            configuration.Invoke(config);

            config.Validate();

            return new FluentNotificationStack(new JustSaying.NotificationStack(config, new MessageSerialisationRegister()), new AmazonQueueCreator());
        }

        public override IPublishEndpointProvider CreatePublisherEndpointProvider(SqsConfiguration subscriptionConfig)
        {
            return new SnsPublishEndpointProvider((IMessagingConfig)Stack.Config, subscriptionConfig);
        }

        public override IPublishSubscribtionEndpointProvider CreateSubscriptiuonEndpointProvider(SqsConfiguration subscriptionConfig)
        {
            return new SqsSubscribtionEndpointProvider(subscriptionConfig, (IMessagingConfig)Stack.Config);
        }
        
        public override void Publish(Message message)
        {
            var config = Stack.Config.AsJustEatConfig();
            message.RaisingComponent = config.Component;
            message.Tenant = config.Tenant;
            base.Publish(message);
        }
    }

    public static class Extensions
    {
        public static IMessagingConfig AsJustEatConfig(this JustSaying.IMessagingConfig config)
        {
            return (IMessagingConfig) config;
        }
    }
}