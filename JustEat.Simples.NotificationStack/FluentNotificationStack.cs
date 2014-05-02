using System;
using Amazon;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.Messages;
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
    public class FluentNotificationStack : JustSayingFluently
    {
        public static string DefaultEndpoint // ToDO: This ain't wired up. Why? Neds checking for back compatability
        {
            get { return RegionEndpoint.EUWest1.SystemName; }
        }

        internal FluentNotificationStack(IAmJustSaying stack, IVerifyAmazonQueues queueCreator): base(stack, queueCreator)
        {
        }

        public static IMayWantMonitoring Register(Action<INotificationStackConfiguration> configuration)
        {
            return JustSayingExtensions.CreateMe.AJustEatBus(configuration);
        }

        public override IPublishEndpointProvider CreatePublisherEndpointProvider(SqsConfiguration subscriptionConfig)
        {
            return new SnsPublishEndpointProvider((IMessagingConfig)Bus.Config, subscriptionConfig);
        }

        public override IPublishSubscribtionEndpointProvider CreateSubscriptiuonEndpointProvider(SqsConfiguration subscriptionConfig)
        {
            return new SqsSubscribtionEndpointProvider(subscriptionConfig, (IMessagingConfig)Bus.Config);
        }
        
        public override void Publish(Message message)
        {
            var config = Bus.Config.AsJustEatConfig();
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