using System;
using System.Collections.Generic;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;

namespace JustSaying
{
    public static class JustSayingFluentlyExtensions
    {
        public static JustSayingFluentlyDependencies WithMessageSubjectProvider(this JustSayingFluentlyDependencies dependencies,
            IMessageSubjectProvider messageSubjectProvider)
        {
            dependencies.MessageSubjectProvider = messageSubjectProvider;
            return dependencies;
        }

        /// <summary>
        /// Note: using this message subject provider may cause incompatibility with applications using prior versions of Just Saying
        /// </summary>
        public static JustSayingFluentlyDependencies WithGenericMessageSubjectProvider(this JustSayingFluentlyDependencies dependencies) =>
            dependencies.WithMessageSubjectProvider(new GenericMessageSubjectProvider());

        public static IMayWantOptionalSettings InRegion(this JustSayingFluentlyDependencies dependencies, string region) => InRegions(dependencies, region);

        public static IMayWantOptionalSettings InRegions(this JustSayingFluentlyDependencies dependencies, params string[] regions) => InRegions(dependencies, regions as IEnumerable<string>);

        public static IMayWantOptionalSettings InRegions(this JustSayingFluentlyDependencies dependencies, IEnumerable<string> regions)
        {
            var config = new MessagingConfig();

            if (dependencies.MessageSubjectProvider != null)
                config.MessageSubjectProvider = dependencies.MessageSubjectProvider;

            if (regions != null)
                foreach (var region in regions)
                {
                    config.Regions.Add(region);
                }

            config.Validate();

            var messageSerializationRegister = new MessageSerializationRegister(config.MessageSubjectProvider);
            var justSayingBus = new JustSayingBus(config, messageSerializationRegister, dependencies.LoggerFactory);

            var awsClientFactoryProxy = new AwsClientFactoryProxy(() => CreateMeABus.DefaultClientFactory());

            var amazonQueueCreator = new AmazonQueueCreator(awsClientFactoryProxy, dependencies.LoggerFactory);
            var bus = new JustSayingFluently(justSayingBus, amazonQueueCreator, awsClientFactoryProxy, dependencies.LoggerFactory);

            bus
                .WithMonitoring(new NullOpMessageMonitor())
                .WithSerializationFactory(new NewtonsoftSerializationFactory());

            return bus;
        }

        public static IFluentSubscription IntoDefaultQueue(this ISubscriberIntoQueue subscriber)
        {
            return subscriber.IntoQueue(string.Empty);
        }

        public static IHaveFulfilledSubscriptionRequirements WithMessageHandlers<T>(
             this IFluentSubscription sub, params IHandlerAsync<T>[] handlers) where T : Message
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            if (handlers.Length == 0)
            {
                throw new ArgumentException("No message handlers specified.", nameof(handlers));
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
