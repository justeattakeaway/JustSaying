using System;
using System.Collections.Generic;
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
        public static JustSayingFluentlyLogging WithMessageSubjectProvider(this JustSayingFluentlyLogging logging,
            IMessageSubjectProvider messageSubjectProvider)
        {
            logging.MessageSubjectProvider = messageSubjectProvider;
            return logging;
        }

        /// <summary>
        /// Note: using this message subject provider may cause incompatibility with applications using prior versions of Just Saying
        /// </summary>
        public static JustSayingFluentlyLogging WithGenericMessageSubjectProvider(this JustSayingFluentlyLogging logging) =>
            logging.WithMessageSubjectProvider(new GenericMessageSubjectProvider());

        public static IMayWantOptionalSettings InRegion(this JustSayingFluentlyLogging logging, string region) => InRegions(logging, region);

        public static IMayWantOptionalSettings InRegions(this JustSayingFluentlyLogging logging, params string[] regions) => InRegions(logging, regions as IEnumerable<string>);

        public static IMayWantOptionalSettings InRegions(this JustSayingFluentlyLogging logging, IEnumerable<string> regions)
        {
            var config = new MessagingConfig();

            if (logging.MessageSubjectProvider != null)
                config.MessageSubjectProvider = logging.MessageSubjectProvider;

            if (regions != null)
                foreach (var region in regions)
                {
                    config.Regions.Add(region);
                }

            config.Validate();

            var messageSerialisationRegister = new MessageSerialisationRegister(config.MessageSubjectProvider);
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
