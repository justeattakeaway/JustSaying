using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions.Kafka.Configuration;
using JustSaying.Extensions.Kafka.Handlers;
using JustSaying.Extensions.Kafka.Messaging;
using JustSaying.Extensions.Kafka.Partitioning;
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

    #region Retry & Dead Letter Topic Configuration

    /// <summary>
    /// Configures the dead letter topic.
    /// Failed messages are sent here after retries are exhausted.
    /// </summary>
    /// <param name="topic">The dead letter topic name.</param>
    public KafkaSubscriptionBuilder<T> WithDeadLetterTopic(string topic)
    {
        _configuration.DeadLetterTopic = topic;
        return this;
    }

    /// <summary>
    /// Configures in-process retry (default, cost-optimized).
    /// Retries happen within the consumer with partition pause.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of retry attempts. Default is 3.</param>
    /// <param name="initialBackoff">Initial backoff delay. Default is 5 seconds.</param>
    /// <param name="exponentialBackoff">Whether to use exponential backoff. Default is true.</param>
    /// <param name="maxBackoff">Maximum backoff delay. Default is 60 seconds.</param>
    public KafkaSubscriptionBuilder<T> WithInProcessRetry(
        int maxAttempts = 3,
        TimeSpan? initialBackoff = null,
        bool exponentialBackoff = true,
        TimeSpan? maxBackoff = null)
    {
        _configuration.Retry.Mode = RetryMode.InProcess;
        _configuration.Retry.MaxRetryAttempts = maxAttempts;
        _configuration.Retry.InitialBackoff = initialBackoff ?? TimeSpan.FromSeconds(5);
        _configuration.Retry.ExponentialBackoff = exponentialBackoff;
        _configuration.Retry.MaxBackoff = maxBackoff ?? TimeSpan.FromSeconds(60);
        return this;
    }

    /// <summary>
    /// Configures topic chaining retry mode (higher cost, non-blocking).
    /// Failed messages are forwarded to a separate topic immediately.
    /// You must configure separate consumers for retry topics.
    /// </summary>
    /// <param name="failureTopic">The topic to forward failed messages to.</param>
    public KafkaSubscriptionBuilder<T> WithTopicChainingRetry(string failureTopic)
    {
        _configuration.Retry.Mode = RetryMode.TopicChaining;
        _configuration.FailureTopic = failureTopic;
        return this;
    }

    /// <summary>
    /// Configures processing delay (for retry topics in topic chaining mode).
    /// Messages will be delayed before processing to allow for backoff.
    /// </summary>
    /// <param name="delay">The delay duration before processing messages.</param>
    public KafkaSubscriptionBuilder<T> WithProcessingDelay(TimeSpan delay)
    {
        _configuration.DelayInMs = (uint)delay.TotalMilliseconds;
        return this;
    }

    /// <summary>
    /// Disables retry - messages go directly to DLT on first failure.
    /// </summary>
    public KafkaSubscriptionBuilder<T> WithNoRetry()
    {
        _configuration.Retry.MaxRetryAttempts = 0;
        return this;
    }

    /// <summary>
    /// Configures a custom failure handler.
    /// When provided, standard retry/DLT behavior is bypassed.
    /// </summary>
    /// <param name="factory">Factory function to create the failure handler.</param>
    public KafkaSubscriptionBuilder<T> WithFailureHandler(
        Func<IServiceProvider, IFailureHandler<T>> factory)
    {
        _configuration.FailureHandlerFactory = sp => (IFailureHandler<Message>)factory(sp);
        return this;
    }

    #endregion

    #region Scaling Configuration

    /// <summary>
    /// Configures the number of consumer instances for horizontal scaling.
    /// </summary>
    /// <param name="count">Number of consumer instances. Default is 1.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Multiple consumers within the same consumer group will share partitions.
    /// Set this to a value less than or equal to the number of partitions for optimal scaling.
    /// Each consumer is registered as a separate IHostedService.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when count is 0.</exception>
    public KafkaSubscriptionBuilder<T> WithNumberOfConsumers(uint count)
    {
        if (count == 0)
            throw new ArgumentException("Number of consumers must be at least 1", nameof(count));

        _configuration.NumberOfConsumers = count;
        return this;
    }

    #endregion

    #region Partitioning Configuration

    /// <summary>
    /// Configures a custom partition key strategy for publishing.
    /// </summary>
    /// <param name="strategy">The partition key strategy to use.</param>
    /// <returns>The builder for chaining.</returns>
    public KafkaSubscriptionBuilder<T> WithPartitionKeyStrategy(IPartitionKeyStrategy strategy)
    {
        _configuration.PartitionKeyStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        return this;
    }

    /// <summary>
    /// Uses message ID as the partition key.
    /// </summary>
    public KafkaSubscriptionBuilder<T> WithMessageIdPartitioning()
    {
        _configuration.PartitionKeyStrategy = MessageIdPartitionKeyStrategy.Instance;
        return this;
    }

    /// <summary>
    /// Uses message.UniqueKey() as the partition key (default behavior for publishers).
    /// </summary>
    public KafkaSubscriptionBuilder<T> WithUniqueKeyPartitioning()
    {
        _configuration.PartitionKeyStrategy = UniqueKeyPartitionKeyStrategy.Instance;
        return this;
    }

    /// <summary>
    /// Uses round-robin partitioning (null key, Kafka distributes evenly).
    /// </summary>
    public KafkaSubscriptionBuilder<T> WithRoundRobinPartitioning()
    {
        _configuration.PartitionKeyStrategy = RoundRobinPartitionKeyStrategy.Instance;
        return this;
    }

    /// <summary>
    /// Uses sticky partitioning - messages go to the same partition for a time period.
    /// </summary>
    /// <param name="stickyDuration">How long to stick to one partition. Default is 1 second.</param>
    public KafkaSubscriptionBuilder<T> WithStickyPartitioning(TimeSpan? stickyDuration = null)
    {
        _configuration.PartitionKeyStrategy = new StickyPartitionKeyStrategy(stickyDuration);
        return this;
    }

    /// <summary>
    /// Uses time-based partitioning - messages are routed based on their timestamp.
    /// </summary>
    /// <param name="windowSize">The time window size. Messages within the same window go to the same partition.</param>
    public KafkaSubscriptionBuilder<T> WithTimeBasedPartitioning(TimeSpan windowSize)
    {
        _configuration.PartitionKeyStrategy = new TimeBasedPartitionKeyStrategy(windowSize);
        return this;
    }

    /// <summary>
    /// Uses consistent hash partitioning based on a message property.
    /// </summary>
    /// <param name="propertySelector">Function to select the property to use as partition key.</param>
    public KafkaSubscriptionBuilder<T> WithConsistentHashPartitioning(Func<T, string> propertySelector)
    {
        _configuration.PartitionKeyStrategy = new ConsistentHashPartitionKeyStrategy<T>(propertySelector);
        return this;
    }

    /// <summary>
    /// Uses a custom delegate for partition key generation.
    /// </summary>
    /// <param name="keySelector">Function that takes (message, topic) and returns the partition key.</param>
    public KafkaSubscriptionBuilder<T> WithCustomPartitioning(Func<T, string, string> keySelector)
    {
        _configuration.PartitionKeyStrategy = new DelegatePartitionKeyStrategy<T>(keySelector);
        return this;
    }

    #endregion

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

    /// <summary>
    /// Gets the topic name.
    /// </summary>
    public string GetTopic() => _topic;

    /// <summary>
    /// Gets the configuration for this subscription.
    /// </summary>
    public KafkaConfiguration GetConfiguration() => _configuration;

    /// <summary>
    /// Gets the number of consumers configured.
    /// </summary>
    public uint GetNumberOfConsumers() => _configuration.NumberOfConsumers;
}
