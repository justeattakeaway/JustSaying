using System;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JustSaying.Fluent
{
    public static class IServiceCollectionExtensions
    {
        // TODO This is here for convenience while protyping, would probably live elsewhere
        // so we don't need to force the dependency on MS' DI types

        public static IServiceCollection AddJustSaying(this IServiceCollection services, params string[] regions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (regions == null)
            {
                throw new ArgumentNullException(nameof(regions));
            }

            return services
                .AddJustSaying(
                    (builder) =>
                    {
                        builder.Messaging((options) => options.WithRegions(regions));
                    });
        }

        public static IServiceCollection AddJustSaying(this IServiceCollection services, Action<MessagingBusBuilder> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return services.AddJustSaying((builder, _) => configure(builder));
        }

        public static IServiceCollection AddJustSaying(this IServiceCollection services, Action<MessagingBusBuilder, IServiceProvider> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            // Register as self so the same singleton instance implements two different interfaces
            services.TryAddSingleton((p) => new ServiceProviderResolver(p));
            services.TryAddSingleton<IHandlerResolver>((p) => p.GetRequiredService<ServiceProviderResolver>());
            services.TryAddSingleton<IServiceResolver>((p) => p.GetRequiredService<ServiceProviderResolver>());

            services.TryAddSingleton<MessagingBusBuilder>();
            services.TryAddSingleton<IAwsClientFactory, DefaultAwsClientFactory>();
            services.TryAddSingleton<IAwsClientFactoryProxy>((p) => new AwsClientFactoryProxy(p.GetRequiredService<IAwsClientFactory>));
            services.TryAddSingleton<IMessagingConfig, MessagingConfig>();
            services.TryAddSingleton<IMessageMonitor, NullOpMessageMonitor>();
            services.TryAddSingleton<IMessageSerialisationFactory, NewtonsoftSerialisationFactory>();
            services.TryAddSingleton<IMessageSubjectProvider, GenericMessageSubjectProvider>();
            services.TryAddSingleton<IVerifyAmazonQueues, AmazonQueueCreator>();
            services.TryAddSingleton<IMessageSerialisationRegister>(
                (p) =>
                {
                    var config = p.GetRequiredService<IMessagingConfig>();
                    return new MessageSerialisationRegister(config.MessageSubjectProvider);
                });

            services.TryAddSingleton(
                (serviceProvider) =>
                {
                    var builder = serviceProvider
                        .GetRequiredService<MessagingBusBuilder>()
                        .WithServiceResolver(new ServiceProviderResolver(serviceProvider));

                    configure(builder, serviceProvider);

                    return builder.Build();
                });

            return services;
        }

        public static IServiceCollection AddJustSayingHandler<TMessage, THandler>(this IServiceCollection services)
            where TMessage : Message
            where THandler : class, IHandlerAsync<TMessage>
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddTransient<IHandlerAsync<TMessage>, THandler>();
            return services;
        }
    }
}
