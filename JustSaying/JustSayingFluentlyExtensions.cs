using System;
using System.Collections.Generic;
using System.Linq;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;

namespace JustSaying
{
    public static class JustSayingFluentlyExtensions
    {
        public static IMayWantOptionalSettings InRegion(this JustSayingFluentlyLogging logging, string region) => InRegions(logging, region);

        public static IMayWantOptionalSettings InRegions(this JustSayingFluentlyLogging logging, IEnumerable<string> regions) => InRegions(logging, regions.ToArray());

        public static IMayWantOptionalSettings InRegions(this JustSayingFluentlyLogging logging, params string[] regions)
        {
            var config = new MessagingConfig();

            if (regions != null)
                foreach (var region in regions)
                {
                    config.Regions.Add(region);
                }

            config.Validate();

            var messageSerialisationRegister = new MessageSerialisationRegister();
            var justSayingBus = new JustSayingBus(config, messageSerialisationRegister, logging.LoggerFactory);

            var awsClientFactoryProxy = new AwsClientFactoryProxy(() => CreateMeABus.DefaultClientFactory());

            var amazonQueueCreator = new AmazonQueueCreator(awsClientFactoryProxy, logging.LoggerFactory);
            var bus = new JustSayingFluently(justSayingBus, amazonQueueCreator, awsClientFactoryProxy, logging.LoggerFactory);

            bus
                .WithMonitoring(new NullOpMessageMonitor())
                .WithSerialisationFactory(new NewtonsoftSerialisationFactory());

            return bus;
        }

        public static IFluentSubscription IntoDefaultQueue(this ISubscriberIntoQueue subscriber)
        {
            return subscriber.IntoQueue(string.Empty);
        }

        public static IHaveFulfilledSubscriptionRequirements WithMessageHandlers<T>(
             this IFluentSubscription sub, params IHandlerAsync<T>[] handlers) where T : Message
        {
            if (handlers.Length == 0)
            {
                throw new ArgumentException("No handlers in list");
            }

            if (handlers.Length == 1)
            {
                sub.WithMessageHandler(handlers[0]);
            }

            var listHandler = new ListHandler<T>(handlers);
            return sub.WithMessageHandler(listHandler);
        }
    }
}