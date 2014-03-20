using System;
using Amazon;
using JustEat.Simples.NotificationStack.AwsTools.QueueCreation;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using SimpleMessageMule;
using SimpleMessageMule.Lookups;
using IPublishEndpointProvider = SimpleMessageMule.Lookups.IPublishEndpointProvider;
using SnsPublishEndpointProvider = JustEat.Simples.NotificationStack.Stack.Lookups.SnsPublishEndpointProvider;
using SqsSubscribtionEndpointProvider = JustEat.Simples.NotificationStack.Stack.Lookups.SqsSubscribtionEndpointProvider;

namespace JustEat.Simples.NotificationStack.Stack
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

            return new FluentNotificationStack(new SimpleMessageMule.NotificationStack(config, new MessageSerialisationRegister()), new AmazonQueueCreator());
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
        public static IMessagingConfig AsJustEatConfig(this SimpleMessageMule.IMessagingConfig config)
        {
            return (IMessagingConfig) config;
        }
    }
}