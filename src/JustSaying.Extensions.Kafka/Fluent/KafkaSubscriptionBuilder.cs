using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions.Kafka.Fluent;

/// <summary>
/// Builder for configuring Kafka topic subscriptions.
/// </summary>
/// <typeparam name="T">The message type to subscribe to.</typeparam>
public class KafkaSubscriptionBuilder<T> : ISubscriptionBuilder<T> where T : Message
{
    private readonly string _topic;
    private readonly KafkaConfiguration _configuration = new();
    private readonly KafkaMessagingConfiguration _globalConfig;
    private Action<HandlerMiddlewareBuilder> _middlewareConfiguration;

    public KafkaSubscriptionBuilder(string topic, KafkaMessagingConfiguration globalConfig = null)
    {
        _topic = topic ?? throw new ArgumentNullException(nameof(topic));
        _globalConfig = globalConfig;

        // Apply global defaults if available
        if (_globalConfig != null)
        {
            _configuration.BootstrapServers = _globalConfig.BootstrapServers;
            _configuration.EnableCloudEvents = _globalConfig.EnableCloudEvents;
            _configuration.CloudEventsSource = _globalConfig.CloudEventsSource;
        }
    }

    /// <summary>
    /// Configures the middleware pipeline for this subscription.
    /// </summary>
    public ISubscriptionBuilder<T> WithMiddlewareConfiguration(Action<HandlerMiddlewareBuilder> middlewareConfiguration)
    {
        _middlewareConfiguration = middlewareConfiguration;
        return this;
    }

    /// <summary>
    /// Configures the subscriptions for the JustSayingBus.
    /// </summary>
    public void Configure(
        JustSayingBus bus,
        IHandlerResolver handlerResolver,
        IServiceResolver serviceResolver,
        IVerifyAmazonQueues creator,
        IAwsClientFactoryProxy awsClientFactoryProxy,
        ILoggerFactory loggerFactory)
    {
        _configuration.Validate();
        
        var serializationFactory = serviceResolver.ResolveService<IMessageBodySerializationFactory>();
        var consumer = new KafkaMessageConsumer(_topic, _configuration, serializationFactory, loggerFactory);
        
        // Get the handler from the handler resolver
        var resolutionContext = new HandlerResolutionContext(_topic);
        var handler = handlerResolver.ResolveHandler<T>(resolutionContext);
        
        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for message type {typeof(T).Name}");
        }

        // Build middleware pipeline
        var middlewareBuilder = new HandlerMiddlewareBuilder(handlerResolver, serviceResolver);
        
        if (_middlewareConfiguration != null)
        {
            _middlewareConfiguration(middlewareBuilder);
        }
        else
        {
            middlewareBuilder.UseDefaults<T>(handler.GetType());
        }

        var middleware = middlewareBuilder.Build();

        // Register the Kafka consumer to run as part of the bus lifecycle
        bus.AddCustomConsumer(async (cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger<KafkaSubscriptionBuilder<T>>();
            logger.LogInformation("Starting Kafka consumer for topic '{Topic}' and message type '{MessageType}'", 
                _topic, typeof(T).Name);
            
            await consumer.StartConsumingWithMiddleware(handler, middleware, cancellationToken).ConfigureAwait(false);
        });

        // Also register middleware with the bus for interrogation
        bus.AddMessageMiddleware<T>(_topic, middleware);
    }

    /// <summary>
    /// Configures the Kafka bootstrap servers.
    /// </summary>
    public KafkaSubscriptionBuilder<T> WithBootstrapServers(string bootstrapServers)
    {
        _configuration.BootstrapServers = bootstrapServers;
        return this;
    }

    /// <summary>
    /// Configures the consumer group ID.
    /// </summary>
    public KafkaSubscriptionBuilder<T> WithGroupId(string groupId)
    {
        _configuration.GroupId = groupId;
        return this;
    }

    /// <summary>
    /// Enables or disables CloudEvents format.
    /// </summary>
    public KafkaSubscriptionBuilder<T> WithCloudEvents(bool enable = true, string source = "urn:justsaying")
    {
        _configuration.EnableCloudEvents = enable;
        _configuration.CloudEventsSource = source;
        return this;
    }

    /// <summary>
    /// Configures the Kafka consumer settings.
    /// </summary>
    public KafkaSubscriptionBuilder<T> WithConsumerConfig(Action<Confluent.Kafka.ConsumerConfig> configure)
    {
        var config = _configuration.ConsumerConfig ?? new Confluent.Kafka.ConsumerConfig();
        configure(config);
        _configuration.ConsumerConfig = config;
        return this;
    }

    /// <summary>
    /// Creates a Kafka consumer from this subscription configuration.
    /// </summary>
    internal KafkaMessageConsumer CreateConsumer(IServiceProvider serviceProvider)
    {
        _configuration.Validate();
        
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var serializationFactory = serviceProvider.GetRequiredService<IMessageBodySerializationFactory>();
        
        return new KafkaMessageConsumer(_topic, _configuration, serializationFactory, loggerFactory);
    }

    /// <summary>
    /// Gets the handler for this subscription.
    /// </summary>
    internal IHandlerAsync<T> GetHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IHandlerAsync<T>>();
    }

    internal string GetTopic() => _topic;
}
