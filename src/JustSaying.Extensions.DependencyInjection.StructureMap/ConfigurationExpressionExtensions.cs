using System;
using System.ComponentModel;
using JustSaying;
using JustSaying.Fluent;
using JustSaying.Messaging;

namespace StructureMap
{
    /// <summary>
    /// A class containing extension methods for the <see cref="ConfigurationExpression"/> class. This class cannot be inherited.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ConfigurationExpressionExtensions
    {
        /// <summary>
        /// Adds JustSaying services to the registry.
        /// </summary>
        /// <param name="registry">The <see cref="ConfigurationExpression"/> to add JustSaying services to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="registry"/> is <see langword="null"/>.
        /// </exception>
        public static void AddJustSaying(this ConfigurationExpression registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            registry.AddJustSaying((_) => { });
        }

        /// <summary>
        /// Adds JustSaying services to the registry.
        /// </summary>
        /// <param name="registry">The <see cref="ConfigurationExpression"/> to add JustSaying services to.</param>
        /// <param name="region">The AWS region(s) to configure.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="registry"/> or <paramref name="region"/> is <see langword="null"/>.
        /// </exception>
        public static void AddJustSaying(this ConfigurationExpression registry, string region)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            registry.AddJustSaying(
                (builder) => builder.Messaging(
                    (options) => options.WithRegion(region)));
        }

        /// <summary>
        /// Adds JustSaying services to the registry.
        /// </summary>
        /// <param name="registry">The <see cref="ConfigurationExpression"/> to add JustSaying services to.</param>
        /// <param name="configure">A delegate to a method to use to configure JustSaying.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="registry"/> or <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public static void AddJustSaying(this ConfigurationExpression registry, Action<MessagingBusBuilder> configure)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            registry.AddJustSaying((builder, _) => configure(builder));
        }

        /// <summary>
        /// Adds JustSaying services to the registry.
        /// </summary>
        /// <param name="registry">The <see cref="ConfigurationExpression"/> to add JustSaying services to.</param>
        /// <param name="configure">A delegate to a method to use to configure JustSaying.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="registry"/> or <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public static void AddJustSaying(this ConfigurationExpression registry, Action<MessagingBusBuilder, IContext> configure)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            registry.AddRegistry<JustSayingRegistry>();

            registry
                .For<MessagingBusBuilder>()
                .Singleton()
                .Use(
                    nameof(MessagingBusBuilder),
                    (context) =>
                    {
                        var builder = new MessagingBusBuilder()
                            .WithServiceResolver(new ContextResolver(context));

                        configure(builder, context);

                        var contributors = context.GetAllInstances<IMessageBusConfigurationContributor>();

                        foreach (var contributor in contributors)
                        {
                            contributor.Configure(builder);
                        }

                        return builder;
                    });

            registry
                .For<IMessagePublisher>()
                .Singleton()
                .Use(
                    nameof(IMessagePublisher),
                    (context) =>
                    {
                        var builder = context.GetInstance<MessagingBusBuilder>();
                        return builder.BuildPublisher();
                    });

            registry
                .For<IMessagingBus>()
                .Singleton()
                .Use(
                    nameof(IMessagingBus),
                    (context) =>
                    {
                        var builder = context.GetInstance<MessagingBusBuilder>();
                        return builder.BuildSubscribers();
                    });
        }
    }
}
