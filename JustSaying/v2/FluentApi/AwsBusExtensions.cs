using System;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.v2.Configuration;
using Microsoft.Extensions.Logging;

namespace JustSaying.v2.FluentApi
{
    public static class AwsBusExtensions
    {
        public static IAwsMessageBusBuilder UsingAws(this BusBuilder builder, Action<IAwsMessageBusConfiguration> busConfig)
        {
            var busBuilder = new AwsMessageBusBuilder();
            busConfig.Invoke(busBuilder);

            return busBuilder;
        }

        public static IAwsMessageBusBuilder UsingAws(this BusBuilder builder, ILoggerFactory loggerFactory, string region, params string[] additionalRegions)
        {
            // TODO: Build using the defaults
            throw new NotImplementedException();
        }
    }

    public static class AwsPublisherExtensions
    {
        public static IAwsMessagePublisher CreatePublishers(this IAwsMessageBusBuilder builder) => builder.CreatePublishers(JustSayingConstants.DEFAULT_PUBLISHER_RETRY_COUNT, JustSayingConstants.DEFAULT_PUBLISHER_RETRY_INTERVAL);

        public static IAwsMessagePublisher AddTopicPublisher<TMessage>(this IAwsMessagePublisher builder) where TMessage : Message
        {
            builder.Add<TMessage>(new AwsTopicPublisherConfiguration());
            return builder;
        }

        public static IAwsMessagePublisher AddTopicPublisher<TMessage>(this IAwsMessagePublisher builder, Action<IAwsTopicPublisherConfiguration> publisherConfig) where TMessage : Message
        {
            builder.Add<TMessage>(publisherConfig.BuildConfig<IAwsTopicPublisherConfiguration, AwsTopicPublisherConfiguration>());
            return builder;
        }

        public static IAwsMessagePublisher AddQueuePublisher<TMessage>(this IAwsMessagePublisher builder, Action<IAwsQueuePublisherConfiguration> publisherConfig) where TMessage : Message
        {
            builder.Add<TMessage>(publisherConfig.BuildConfig<IAwsQueuePublisherConfiguration, AwsQueuePublisherConfiguration>());
            return builder;
        }
    }

    public static class AwsSubscriberExtensions
    {
        public static AwsPendingMessageSubscriber<IAwsTopicSubscriberConfiguration> AddTopicSubscriber(this IAwsMessageSubscriber builder, Action<IAwsTopicSubscriberConfiguration> subscriberConfig)
        {
            return new AwsPendingMessageSubscriber<IAwsTopicSubscriberConfiguration>(builder, subscriberConfig);
        }

        public static AwsPendingMessageSubscriber<IAwsQueueSubscriberConfiguration> AddQueueSubscriber(this IAwsMessageSubscriber builder, Action<IAwsQueueSubscriberConfiguration> subscriberConfig)
        {
            return new AwsPendingMessageSubscriber<IAwsQueueSubscriberConfiguration>(builder, subscriberConfig);
        }

        public static IAwsMessageSubscriber WithHandler<TMessage>(this AwsPendingMessageSubscriber<IAwsTopicSubscriberConfiguration> pendingSubscription, IHandlerAsync<TMessage> handler) where TMessage : Message
        {
            pendingSubscription.Builder.Add(pendingSubscription.Configuration.BuildConfig<IAwsTopicSubscriberConfiguration, AwsTopicSubscriberConfiguration>(), handler);
            return pendingSubscription.Builder;
        }

        public static IAwsMessageSubscriber WithHandler<TMessage>(this AwsPendingMessageSubscriber<IAwsTopicSubscriberConfiguration> pendingSubscription, IHandlerResolver handlerResolver) where TMessage : Message
        {
            pendingSubscription.Builder.Add<TMessage>(pendingSubscription.Configuration.BuildConfig<IAwsTopicSubscriberConfiguration, AwsTopicSubscriberConfiguration>(), handlerResolver);
            return pendingSubscription.Builder;
        }

        public static IAwsMessageSubscriber WithHandler<TMessage>(this AwsPendingMessageSubscriber<IAwsQueueSubscriberConfiguration> pendingSubscription, IHandlerAsync<TMessage> handler) where TMessage : Message
        {
            pendingSubscription.Builder.Add(pendingSubscription.Configuration.BuildConfig<IAwsQueueSubscriberConfiguration, AwsQueueSubscriberConfiguration>(), handler);
            return pendingSubscription.Builder;
        }

        public static IAwsMessageSubscriber WithHandler<TMessage>(this AwsPendingMessageSubscriber<IAwsQueueSubscriberConfiguration> pendingSubscription, IHandlerResolver handlerResolver) where TMessage : Message
        {
            pendingSubscription.Builder.Add<TMessage>(pendingSubscription.Configuration.BuildConfig<IAwsQueueSubscriberConfiguration, AwsQueueSubscriberConfiguration>(), handlerResolver);
            return pendingSubscription.Builder;
        }
    }

    public static class AwsConfigExtensions
    {
        public static TBuiltConfig BuildConfig<TPendingConfig, TBuiltConfig>(this Action<TPendingConfig> pendingConfig) where TBuiltConfig : TPendingConfig, new()
        {
            var builtConfig = new TBuiltConfig();
            pendingConfig.Invoke(builtConfig);

            return builtConfig;
        }
    }
}