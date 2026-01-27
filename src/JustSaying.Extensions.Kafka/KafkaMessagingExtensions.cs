using JustSaying;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Factory;
using JustSaying.Extensions.Kafka.Fluent;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Extensions.Kafka.Monitoring;
using JustSaying.Extensions.Kafka.Tracing;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring Kafka transport in JustSaying.
/// </summary>
public static class KafkaMessagingExtensions
{
    /// <summary>
    /// Adds Kafka as a message publisher for the specified message type.
    /// </summary>
    public static IServiceCollection WithKafkaPublisher<T>(
        this IServiceCollection services,
        string topic,
        Action<KafkaConfiguration> configure) where T : Message
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic name is required.", nameof(topic));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var kafkaConfig = new KafkaConfiguration();
        configure(kafkaConfig);
        kafkaConfig.Validate();

        // Register the publisher
        services.AddSingleton<IMessagePublisher>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var serializationFactory = sp.GetRequiredService<IMessageBodySerializationFactory>();
            
            return new KafkaMessagePublisher(
                topic,
                kafkaConfig,
                serializationFactory,
                loggerFactory);
        });

        return services;
    }

    /// <summary>
    /// Adds Kafka as a message batch publisher for the specified message type.
    /// </summary>
    public static IServiceCollection WithKafkaBatchPublisher<T>(
        this IServiceCollection services,
        string topic,
        Action<KafkaConfiguration> configure) where T : Message
    {
        // The KafkaMessagePublisher implements both IMessagePublisher and IMessageBatchPublisher
        return services.WithKafkaPublisher<T>(topic, configure);
    }

    /// <summary>
    /// Adds a Kafka topic subscription for the specified message type.
    /// </summary>
    public static IServiceCollection AddKafkaSubscription<T>(
        this IServiceCollection services,
        string topic,
        Action<KafkaSubscriptionBuilder<T>> configure) where T : Message
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic name is required.", nameof(topic));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var kafkaBuilder = new KafkaSubscriptionBuilder<T>(topic);
        configure(kafkaBuilder);

        // Register the subscription in the DI container
        services.AddSingleton(kafkaBuilder);

        return services;
    }

    /// <summary>
    /// Creates a Kafka message consumer for the specified topic.
    /// </summary>
    public static KafkaMessageConsumer CreateKafkaConsumer(
        this IServiceProvider serviceProvider,
        string topic,
        Action<KafkaConfiguration> configure)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic name is required.", nameof(topic));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var kafkaConfig = new KafkaConfiguration();
        configure(kafkaConfig);
        kafkaConfig.Validate();

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var serializationFactory = serviceProvider.GetRequiredService<IMessageBodySerializationFactory>();

        return new KafkaMessageConsumer(
            topic,
            kafkaConfig,
            serializationFactory,
            loggerFactory);
    }

    #region Factory Registration

    /// <summary>
    /// Adds the default Kafka consumer and producer factories.
    /// Call this to enable factory-based consumer/producer creation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKafkaFactories(this IServiceCollection services)
    {
        services.TryAddSingleton<IKafkaConsumerFactory, KafkaConsumerFactory>();
        services.TryAddSingleton<IKafkaProducerFactory, KafkaProducerFactory>();
        return services;
    }

    /// <summary>
    /// Adds a custom Kafka consumer factory.
    /// </summary>
    /// <typeparam name="TFactory">The factory implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKafkaConsumerFactory<TFactory>(this IServiceCollection services)
        where TFactory : class, IKafkaConsumerFactory
    {
        services.AddSingleton<IKafkaConsumerFactory, TFactory>();
        return services;
    }

    /// <summary>
    /// Adds a custom Kafka producer factory.
    /// </summary>
    /// <typeparam name="TFactory">The factory implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKafkaProducerFactory<TFactory>(this IServiceCollection services)
        where TFactory : class, IKafkaProducerFactory
    {
        services.AddSingleton<IKafkaProducerFactory, TFactory>();
        return services;
    }

    #endregion

    #region Consumer Monitoring

    /// <summary>
    /// Adds a custom Kafka consumer monitor.
    /// Multiple monitors can be registered - they will all be invoked.
    /// </summary>
    /// <typeparam name="TMonitor">The monitor implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKafkaConsumerMonitor<TMonitor>(this IServiceCollection services)
        where TMonitor : class, IKafkaConsumerMonitor
    {
        services.TryAddSingleton<IKafkaConsumerMonitor, CompositeKafkaConsumerMonitor>();
        services.AddSingleton<IKafkaConsumerMonitor, TMonitor>();
        return services;
    }

    /// <summary>
    /// Adds the default logging-based consumer monitor.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKafkaLoggingMonitor(this IServiceCollection services)
    {
        return services.AddKafkaConsumerMonitor<LoggingKafkaConsumerMonitor>();
    }

    /// <summary>
    /// Adds the OpenTelemetry-based consumer monitor.
    /// Emits metrics via System.Diagnostics.Metrics for collection by OpenTelemetry exporters.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Configure OpenTelemetry to collect these metrics by adding the meter:
    /// <code>
    /// builder.Services.AddOpenTelemetry()
    ///     .WithMetrics(metrics => metrics
    ///         .AddMeter("JustSaying.Kafka"));
    /// </code>
    /// </remarks>
    public static IServiceCollection AddKafkaOpenTelemetryMetrics(this IServiceCollection services)
    {
        return services.AddKafkaConsumerMonitor<OpenTelemetryKafkaConsumerMonitor>();
    }

    /// <summary>
    /// Adds distributed tracing support for Kafka consumers.
    /// Creates Activity spans for message consumption using System.Diagnostics.Activity.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Configure OpenTelemetry to collect traces by adding the activity source:
    /// <code>
    /// builder.Services.AddOpenTelemetry()
    ///     .WithTracing(tracing => tracing
    ///         .AddSource("JustSaying.Kafka"));
    /// </code>
    /// </remarks>
    public static IServiceCollection AddKafkaDistributedTracing(this IServiceCollection services)
    {
        return services.AddKafkaConsumerMonitor<TracingKafkaConsumerMonitor>();
    }

    /// <summary>
    /// Adds both OpenTelemetry metrics and distributed tracing for Kafka.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Configure OpenTelemetry to collect both metrics and traces:
    /// <code>
    /// builder.Services.AddOpenTelemetry()
    ///     .WithMetrics(metrics => metrics.AddMeter("JustSaying.Kafka"))
    ///     .WithTracing(tracing => tracing.AddSource("JustSaying.Kafka"));
    /// </code>
    /// </remarks>
    public static IServiceCollection AddKafkaOpenTelemetry(this IServiceCollection services)
    {
        services.AddKafkaOpenTelemetryMetrics();
        services.AddKafkaDistributedTracing();
        return services;
    }

    /// <summary>
    /// Gets the configured consumer monitor (composite or null).
    /// For internal use by consumers.
    /// </summary>
    internal static IKafkaConsumerMonitor GetKafkaConsumerMonitor(this IServiceProvider serviceProvider)
    {
        var monitors = serviceProvider.GetServices<IKafkaConsumerMonitor>()?.ToArray();

        if (monitors == null || monitors.Length == 0)
            return NullKafkaConsumerMonitor.Instance;

        if (monitors.Length == 1)
            return monitors[0];

        // Filter out any CompositeKafkaConsumerMonitor instances to avoid nesting
        var actualMonitors = monitors.Where(m => m is not CompositeKafkaConsumerMonitor).ToList();

        if (actualMonitors.Count == 0)
            return NullKafkaConsumerMonitor.Instance;

        if (actualMonitors.Count == 1)
            return actualMonitors[0];

        return new CompositeKafkaConsumerMonitor(actualMonitors);
    }

    #endregion

    #region Message Context Accessor

    /// <summary>
    /// Adds the Kafka message context accessor for accessing rich message metadata in handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Usage in handlers:
    /// <code>
    /// public class OrderHandler : IHandlerAsync&lt;OrderEvent&gt;
    /// {
    ///     private readonly IKafkaMessageContextAccessor _contextAccessor;
    ///     
    ///     public OrderHandler(IKafkaMessageContextAccessor contextAccessor)
    ///     {
    ///         _contextAccessor = contextAccessor;
    ///     }
    ///     
    ///     public Task&lt;bool&gt; Handle(OrderEvent message)
    ///     {
    ///         var ctx = _contextAccessor.Context;
    ///         _logger.LogInformation("Partition: {P}, Lag: {L}ms", 
    ///             ctx.Partition, ctx.LagMilliseconds);
    ///         return Task.FromResult(true);
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddKafkaMessageContextAccessor(this IServiceCollection services)
    {
        services.TryAddSingleton<IKafkaMessageContextAccessor, KafkaMessageContextAccessor>();
        return services;
    }

    /// <summary>
    /// Gets the message context accessor from the service provider.
    /// For internal use by consumers.
    /// </summary>
    internal static IKafkaMessageContextAccessor GetKafkaMessageContextAccessor(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<IKafkaMessageContextAccessor>();
    }

    #endregion

    #region Typed Producer Registration

    /// <summary>
    /// Adds a typed Kafka producer with the specified configuration.
    /// </summary>
    /// <typeparam name="TProducerType">A marker type to identify this producer configuration.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Use marker types to register multiple producers with different configurations:
    /// <code>
    /// public class OrderServiceProducer { }
    /// public class PaymentServiceProducer { }
    /// 
    /// services.AddKafkaProducer&lt;OrderServiceProducer&gt;(config => {
    ///     config.BootstrapServers = "localhost:9092";
    /// });
    /// services.AddKafkaProducer&lt;PaymentServiceProducer&gt;(config => {
    ///     config.BootstrapServers = "payments-cluster:9092";
    /// });
    /// </code>
    /// </remarks>
    public static IServiceCollection AddKafkaProducer<TProducerType>(
        this IServiceCollection services,
        Action<KafkaConfiguration> configure) where TProducerType : class
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var configuration = new KafkaConfiguration();
        configure(configuration);
        configuration.Validate();

        // Ensure factories are registered
        services.AddKafkaFactories();

        // Register configuration for this producer type
        services.AddSingleton(sp => new TypedKafkaConfiguration<TProducerType>(configuration));

        // Register the typed producer
        services.AddSingleton<IKafkaProducer<TProducerType>>(sp =>
        {
            var config = sp.GetRequiredService<TypedKafkaConfiguration<TProducerType>>().Configuration;
            var factory = sp.GetRequiredService<IKafkaProducerFactory>();
            var serializationFactory = sp.GetRequiredService<IMessageBodySerializationFactory>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return new KafkaProducer<TProducerType>(config, factory, serializationFactory, loggerFactory);
        });

        return services;
    }

    #endregion

    #region Consumer Worker Registration

    /// <summary>
    /// Adds a Kafka consumer worker as a hosted service.
    /// </summary>
    /// <typeparam name="T">The message type to consume.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="topic">The topic to consume from.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKafkaConsumerWorker<T>(
        this IServiceCollection services,
        string topic,
        Action<KafkaConfiguration> configure) where T : Message
    {
        if (string.IsNullOrEmpty(topic))
            throw new ArgumentException("Topic is required", nameof(topic));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var configuration = new KafkaConfiguration();
        configure(configuration);
        configuration.Validate();

        var numberOfConsumers = configuration.NumberOfConsumers;

        // Register multiple consumer workers if configured
        for (uint i = 0; i < numberOfConsumers; i++)
        {
            var consumerId = numberOfConsumers > 1 
                ? $"{topic}-consumer-{i}" 
                : $"{topic}-consumer";

            var capturedConsumerId = consumerId;
            var capturedConfig = configuration;

            // Register as singleton IHostedService for all frameworks
            services.AddSingleton<IHostedService>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                return new KafkaConsumerWorker<T>(
                    capturedConsumerId,
                    topic,
                    capturedConfig,
                    sp,
                    loggerFactory);
            });
        }

        return services;
    }

    #endregion
}

/// <summary>
/// Wrapper to hold typed producer configuration in DI.
/// </summary>
internal class TypedKafkaConfiguration<TProducerType> where TProducerType : class
{
    public KafkaConfiguration Configuration { get; }

    public TypedKafkaConfiguration(KafkaConfiguration configuration)
    {
        Configuration = configuration;
    }
}
