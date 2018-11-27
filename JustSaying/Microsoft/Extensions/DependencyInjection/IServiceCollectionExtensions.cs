using System;
using System.ComponentModel;
using JustSaying;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A class containing extension methods for the <see cref="IServiceCollection"/> interface. This class cannot be inherited.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IServiceCollectionExtensions
    {
        // TODO This is here for convenience while protyping, would probably live elsewhere
        // so we don't need to force the dependency on MS' DI types

        /// <summary>
        /// Adds JustSaying services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add JustSaying services to.</param>
        /// <param name="regions">The AWS region(s) to configure.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> specified by <paramref name="services"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> or <paramref name="regions"/> is <see langword="null"/>.
        /// </exception>
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

            return services.AddJustSaying(
                (builder) => builder.Messaging(
                    (options) => options.WithRegions(regions)));
        }

        /// <summary>
        /// Adds JustSaying services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add JustSaying services to.</param>
        /// <param name="configure">A delegate to a method to use to configure JustSaying.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> specified by <paramref name="services"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
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

        /// <summary>
        /// Adds JustSaying services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add JustSaying services to.</param>
        /// <param name="configure">A delegate to a method to use to configure JustSaying.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> specified by <paramref name="services"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
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

            services.TryAddSingleton<IAwsClientFactory, DefaultAwsClientFactory>();
            services.TryAddSingleton<IAwsClientFactoryProxy>((p) => new AwsClientFactoryProxy(p.GetRequiredService<IAwsClientFactory>));
            services.TryAddSingleton<IMessagingConfig, MessagingConfig>();
            services.TryAddSingleton<IMessageMonitor, NullOpMessageMonitor>();
            services.TryAddSingleton<IMessageSerializationFactory, NewtonsoftSerializationFactory>();
            services.TryAddSingleton<IMessageSubjectProvider, GenericMessageSubjectProvider>();
            services.TryAddSingleton<IVerifyAmazonQueues, AmazonQueueCreator>();
            services.TryAddSingleton<IMessageSerializationRegister>(
                (p) =>
                {
                    var config = p.GetRequiredService<IMessagingConfig>();
                    return new MessageSerializationRegister(config.MessageSubjectProvider);
                });

            services.TryAddSingleton(
                (serviceProvider) =>
                {
                    var builder = new MessagingBusBuilder()
                        .WithServiceResolver(new ServiceProviderResolver(serviceProvider));

                    configure(builder, serviceProvider);

                    return builder;
                });

            services.TryAddSingleton(
                (serviceProvider) =>
                {
                    var builder = serviceProvider.GetRequiredService<MessagingBusBuilder>();
                    return builder.BuildPublisher();
                });

            services.TryAddSingleton(
                (serviceProvider) =>
                {
                    var builder = serviceProvider.GetRequiredService<MessagingBusBuilder>();
                    return builder.BuildSubscribers();
                });

            return services;
        }

        /// <summary>
        /// Adds a JustSaying message handler to the service collection.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message handled.</typeparam>
        /// <typeparam name="THandler">The type of the message handler to register.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the message handler to.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> specified by <paramref name="services"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
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
