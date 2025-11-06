using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JustSaying;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using JustSaying.Naming;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A class containing extension methods for the <see cref="IServiceCollection"/> interface. This class cannot be inherited.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds JustSaying services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add JustSaying services to.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> specified by <paramref name="services"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddJustSaying(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddJustSaying((_) => { });
        }

        /// <summary>
        /// Adds JustSaying services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add JustSaying services to.</param>
        /// <param name="region">The AWS region to configure.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> specified by <paramref name="services"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> or <paramref name="region"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddJustSaying(this IServiceCollection services, string region)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentException("region must not be null or empty" ,nameof(region));
            }

            return services.AddJustSaying(
                (builder) => builder.Messaging(
                    (options) => options.WithRegion(region)));
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

            services.AddSingleton<MessageContextAccessor>();
            services.TryAddSingleton<IMessageContextAccessor>(serviceProvider => serviceProvider.GetRequiredService<MessageContextAccessor>());
            services.TryAddSingleton<IMessageContextReader>(serviceProvider => serviceProvider.GetRequiredService<MessageContextAccessor>());

            services.TryAddSingleton<IMessageSerializationFactory, NewtonsoftSerializationFactory>();
            services.TryAddSingleton<IMessageSubjectProvider, GenericMessageSubjectProvider>();
            services.TryAddSingleton<IVerifyAmazonQueues, AmazonQueueCreator>();
            services.TryAddSingleton<IMessageSerializationRegister>(
                (p) =>
                {
                    var config = p.GetRequiredService<IMessagingConfig>();
                    var serializerFactory = p.GetRequiredService<IMessageSerializationFactory>();
                    return new MessageSerializationRegister(config.MessageSubjectProvider, serializerFactory);
                });

            services.TryAddSingleton(
                (serviceProvider) =>
                {
                    var builder = new MessagingBusBuilder()
                        .WithServiceResolver(new ServiceProviderResolver(serviceProvider));

                    configure(builder, serviceProvider);

                    var contributors = serviceProvider.GetServices<IMessageBusConfigurationContributor>();

                    foreach (var contributor in contributors)
                    {
                        contributor.Configure(builder);
                    }

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

            services.AddSingleton<DefaultNamingConventions>();
            services.TryAddSingleton<ITopicNamingConvention>((s) => s.GetRequiredService<DefaultNamingConventions>());
            services.TryAddSingleton<IQueueNamingConvention>((s) => s.GetRequiredService<DefaultNamingConventions>());

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

        /// <summary>
        /// Adds a JustSaying message handler to the service collection that uses the specified handlers.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message handled.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the message handler to.</param>
        /// <param name="handlers">The message handlers to use.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> specified by <paramref name="services"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> or <paramref name="handlers"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="handlers"/> contains no handlers.
        /// </exception>
        public static IServiceCollection AddJustSayingHandlers<TMessage>(
            this IServiceCollection services,
            IEnumerable<IHandlerAsync<TMessage>> handlers)
            where TMessage : Message
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            var enumeratedHandlers = handlers.ToList();

            if (enumeratedHandlers.Count < 1)
            {
                throw new ArgumentException("At least one message handler must be specified.", nameof(handlers));
            }

            if (enumeratedHandlers.Count == 1)
            {
                services.TryAddTransient((_) => enumeratedHandlers[0]);
            }
            else
            {
                services.TryAddTransient<IHandlerAsync<TMessage>>((_) => new ListHandler<TMessage>(enumeratedHandlers));
            }

            return services;
        }

        /// <summary>
        /// Configures JustSaying using the specified service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure JustSaying with.</param>
        /// <param name="configure">A delegate to a method to use to configure JustSaying.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> specified by <paramref name="services"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection ConfigureJustSaying(this IServiceCollection services, Action<MessagingBusBuilder> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return services.AddSingleton<IMessageBusConfigurationContributor>(new DelegatingConfigurationContributor(configure));
        }

        private sealed class DelegatingConfigurationContributor : IMessageBusConfigurationContributor
        {
            private readonly Action<MessagingBusBuilder> _configure;

            internal DelegatingConfigurationContributor(Action<MessagingBusBuilder> configure)
            {
                _configure = configure;
            }

            public void Configure(MessagingBusBuilder builder)
                => _configure(builder);
        }
    }
}
