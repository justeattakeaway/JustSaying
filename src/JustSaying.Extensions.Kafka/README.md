# JustSaying.Extensions.Kafka

[![NuGet](https://img.shields.io/nuget/v/JustSaying.Extensions.Kafka.svg)](https://www.nuget.org/packages/JustSaying.Extensions.Kafka/)

JustSaying extension that adds Apache Kafka support with CloudEvents compliance while maintaining full compatibility with existing JustSaying `Message` types.

## Features

- ✅ **CloudEvents Support**: Fully compliant with CloudEvents v1.0 specification
- ✅ **Backward Compatible**: Works seamlessly with existing JustSaying `Message` types
- ✅ **Dual Mode**: Supports both CloudEvents format and standard JSON serialization
- ✅ **Producer & Consumer**: Full support for publishing and consuming messages
- ✅ **Batch Publishing**: Efficient batch message publishing
- ✅ **Fluent API**: Easy-to-use fluent configuration
- ✅ **Subscription Pattern**: Declarative consumer configuration matching JustSaying patterns
- ✅ **Metadata Preservation**: Maintains all JustSaying message metadata
- ✅ **Dead Letter Topics**: Configurable DLT with in-process retry or topic chaining modes
- ✅ **Consumer Monitoring**: Extensible monitoring interface for metrics and observability
- ✅ **Factory Pattern**: Testable consumer/producer creation via factory interfaces
- ✅ **Partition Rebalance**: Proper handling of Kafka partition reassignment
- ✅ **OpenTelemetry Metrics**: Built-in metrics via System.Diagnostics.Metrics
- ✅ **Horizontal Scaling**: Multiple consumer instances per topic
- ✅ **Distributed Tracing**: OpenTelemetry tracing with W3C Trace Context propagation
- ✅ **Advanced Partitioning**: Configurable partition key strategies (round-robin, sticky, time-based, custom)
- ✅ **Stream Processing**: Lightweight Kafka Streams-like abstractions (filter, map, flatmap, windowing)

## Installation

```bash
dotnet add package JustSaying.Extensions.Kafka
```

## Quick Start

### Publishing Messages

```csharp
using JustSaying;
using JustSaying.Extensions.Kafka;
using Microsoft.Extensions.DependencyInjection;

// Setup with CloudEvents support
var services = new ServiceCollection();

var publisher = services
    .AddJustSaying(config =>
    {
        config.WithLogging(new LoggerFactory());
        config.Messaging(x => x.WithRegion("us-east-1"));
        
        // Add Kafka publisher with CloudEvents
        config.WithKafkaPublisher<OrderPlacedEvent>("order-events", kafka =>
        {
            kafka.BootstrapServers = "localhost:9092";
            kafka.EnableCloudEvents = true; // Default
            kafka.CloudEventsSource = "urn:myapp:orders";
        });
    })
    .BuildPublisher();

// Publish a message
var message = new OrderPlacedEvent
{
    OrderId = "12345",
    CustomerId = "customer-1",
    Amount = 99.99m
};

await publisher.PublishAsync(message);
```

### Consuming Messages (Recommended: Subscription Pattern)

The recommended approach is to use the declarative subscription pattern:

```csharp
// Register message handlers
services.AddSingleton<OrderPlacedEventHandler>();

// Configure subscriptions
services.AddJustSaying(config =>
{
    config.Subscriptions(sub =>
    {
        sub.ForKafkaTopic<OrderPlacedEvent>("order-events", kafka =>
        {
            kafka.WithBootstrapServers("localhost:9092")
                 .WithGroupId("order-processor")
                 .WithCloudEvents(true, "urn:myapp:orders");
        });
    });
});

// Create a background service to start consumers
public class KafkaBusService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private JustSayingBus _bus;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _bus = _serviceProvider.GetRequiredService<JustSayingBus>();
        await _bus.StartKafkaConsumersAsync(ct);
    }

    public override void Dispose()
    {
        _bus?.DisposeKafkaConsumers();
        base.Dispose();
    }
}

// Register the service
services.AddHostedService<KafkaBusService>();
```

### Consuming Messages (Manual)

For manual control, you can create consumers directly:

```csharp
using JustSaying.Extensions.Kafka;
using JustSaying.Messaging.MessageHandling;

// Create a message handler
public class OrderPlacedEventHandler : IHandlerAsync<OrderPlacedEvent>
{
    public async Task<bool> Handle(OrderPlacedEvent message)
    {
        // Process your message
        Console.WriteLine($"Order {message.OrderId} received!");
        return true;
    }
}

// Setup consumer
var consumer = serviceProvider.CreateKafkaConsumer("order-events", kafka =>
{
    kafka.BootstrapServers = "localhost:9092";
    kafka.GroupId = "order-processor";
    kafka.EnableCloudEvents = true;
});

var handler = new OrderPlacedEventHandler();
await consumer.StartAsync(handler, cancellationToken);
```

## Configuration

### CloudEvents Format (Default)

By default, messages are published and consumed using the CloudEvents specification:

```csharp
config.WithKafkaPublisher<MyMessage>("my-topic", kafka =>
{
    kafka.BootstrapServers = "localhost:9092";
    kafka.EnableCloudEvents = true; // This is the default
    kafka.CloudEventsSource = "urn:myapp:service";
});
```

**CloudEvents Message Structure:**
```json
{
  "specversion": "1.0",
  "id": "3c8f5c4e-7b2a-4d9e-8f1a-2b3c4d5e6f7a",
  "type": "MyApp.Events.OrderPlacedEvent",
  "source": "urn:myapp:service",
  "time": "2024-12-02T10:30:00Z",
  "datacontenttype": "application/json",
  "subject": "OrderPlacedEvent",
  "data": {
    "orderId": "12345",
    "customerId": "customer-1",
    "amount": 99.99
  },
  "raisingcomponent": "OrderService",
  "tenant": "tenant-1"
}
```

### Standard JSON Format (Backward Compatibility)

You can disable CloudEvents for backward compatibility:

```csharp
config.WithKafkaPublisher<MyMessage>("my-topic", kafka =>
{
    kafka.BootstrapServers = "localhost:9092";
    kafka.EnableCloudEvents = false;
});
```

### Advanced Configuration

```csharp
config.WithKafkaPublisher<MyMessage>("my-topic", kafka =>
{
    kafka.BootstrapServers = "localhost:9092,localhost:9093,localhost:9094";
    kafka.EnableCloudEvents = true;
    kafka.CloudEventsSource = "urn:myapp:orders";
    
    // Custom producer configuration
    kafka.ProducerConfig = new ProducerConfig
    {
        Acks = Acks.All,
        EnableIdempotence = true,
        MaxInFlight = 5,
        MessageSendMaxRetries = 3,
        CompressionType = CompressionType.Snappy
    };
});
```

### Consumer Configuration

```csharp
var consumer = serviceProvider.CreateKafkaConsumer("my-topic", kafka =>
{
    kafka.BootstrapServers = "localhost:9092";
    kafka.GroupId = "my-consumer-group";
    kafka.EnableCloudEvents = true;
    
    // Custom consumer configuration
    kafka.ConsumerConfig = new ConsumerConfig
    {
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false,
        MaxPollIntervalMs = 300000,
        SessionTimeoutMs = 10000
    };
});
```

## Consumer Monitoring

The Kafka extension provides an extensible monitoring interface for tracking consumer events such as message receipt, processing success, failures, and dead-lettering.

### Built-in Logging Monitor

```csharp
// Add the logging monitor (logs all consumer events)
services.AddKafkaLoggingMonitor();
```

### Custom Monitors

Implement `IKafkaConsumerMonitor` to collect custom metrics or integrate with your monitoring system:

```csharp
public class MyMetricsMonitor : IKafkaConsumerMonitor
{
    private readonly IMetricsCollector _metrics;

    public MyMetricsMonitor(IMetricsCollector metrics)
    {
        _metrics = metrics;
    }

    public void OnMessageReceived<T>(MessageReceivedContext<T> context) where T : Message
    {
        _metrics.Gauge("kafka.consumer.lag_ms", context.LagMilliseconds, 
            new[] { $"topic:{context.Topic}", $"partition:{context.Partition}" });
    }

    public void OnMessageProcessed<T>(MessageProcessedContext<T> context) where T : Message
    {
        _metrics.Timer("kafka.consumer.processing_ms", context.ProcessingDuration.TotalMilliseconds,
            new[] { $"topic:{context.Topic}" });
    }

    public void OnMessageFailed<T>(MessageFailedContext<T> context) where T : Message
    {
        _metrics.Increment("kafka.consumer.failures",
            new[] { $"topic:{context.Topic}", $"will_retry:{context.WillRetry}" });
    }

    public void OnMessageDeadLettered<T>(MessageDeadLetteredContext<T> context) where T : Message
    {
        _metrics.Increment("kafka.consumer.dead_lettered",
            new[] { $"topic:{context.Topic}", $"dlt:{context.DeadLetterTopic}" });
    }
}

// Register your custom monitor
services.AddKafkaConsumerMonitor<MyMetricsMonitor>();
```

### Available Context Properties

| Context Type | Properties |
|--------------|------------|
| `MessageReceivedContext<T>` | Topic, Partition, Offset, MessageTimestamp, ReceivedAt, LagMilliseconds, Message |
| `MessageProcessedContext<T>` | Topic, Partition, Offset, Message, ProcessingDuration, RetryAttempt |
| `MessageFailedContext<T>` | Topic, Partition, Offset, Message, Exception, RetryAttempt, WillRetry |
| `MessageDeadLetteredContext<T>` | Topic, DeadLetterTopic, Partition, Offset, Message, Exception, TotalAttempts |

Multiple monitors can be registered and will all be invoked for each event.

## Factory Pattern

The extension provides factory interfaces for creating Kafka consumers and producers. This enables easier testing and customization.

### Default Factories

```csharp
// Register default factories
services.AddKafkaFactories();
```

### Custom Factories

Implement `IKafkaConsumerFactory` or `IKafkaProducerFactory` for custom consumer/producer creation:

```csharp
public class MyConsumerFactory : IKafkaConsumerFactory
{
    public IConsumer<string, byte[]> CreateConsumer(
        KafkaConfiguration configuration,
        string consumerId)
    {
        var config = configuration.GetConsumerConfig();
        
        // Custom configuration
        config.StatisticsIntervalMs = 5000;
        
        return new ConsumerBuilder<string, byte[]>(config)
            .SetStatisticsHandler((_, json) => LogStats(json))
            .Build();
    }
}

// Register custom factory
services.AddKafkaConsumerFactory<MyConsumerFactory>();
```

### Partition Rebalance Handling

The consumer automatically handles Kafka partition rebalances:

- **Partition Assignment**: Cleans up stale tracking state
- **Partition Revoked**: Cancels pending delayed tasks and resumes paused partitions
- **Partition Lost**: Same cleanup as revoked (for session timeout scenarios)

This ensures that:
- Delayed messages are not processed after partition reassignment
- Paused partitions are properly resumed before revocation
- In-process retries are cancelled when the partition is reassigned

## OpenTelemetry Metrics

The extension provides built-in OpenTelemetry metrics via `System.Diagnostics.Metrics`.

### Enable OpenTelemetry Metrics

```csharp
// Register the OpenTelemetry metrics monitor
services.AddKafkaOpenTelemetryMetrics();

// Configure OpenTelemetry to collect the metrics
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("JustSaying.Kafka"));
```

### Available Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `kafka.consumer.messages.received` | Counter | Total messages received |
| `kafka.consumer.messages.processed` | Counter | Total messages successfully processed |
| `kafka.consumer.messages.failed` | Counter | Total messages that failed processing |
| `kafka.consumer.messages.dead_lettered` | Counter | Total messages sent to DLT |
| `kafka.consumer.retry.attempts` | Counter | Total retry attempts |
| `kafka.consumer.lag` | Histogram | Consumer lag in milliseconds |
| `kafka.consumer.processing.duration` | Histogram | Processing time in milliseconds |

### Tags

All metrics include `topic` and `partition` tags. Failed messages also include `exception_type` and `will_retry` tags.

## Horizontal Scaling

Configure multiple consumer instances per topic for horizontal scaling:

```csharp
config.Subscriptions(sub =>
{
    sub.ForKafkaTopic<OrderEvent>("orders", kafka =>
    {
        kafka.WithBootstrapServers("localhost:9092")
             .WithGroupId("order-processor")
             .WithNumberOfConsumers(4);  // 4 consumer instances
    });
});
```

### Best Practices

- Set `NumberOfConsumers` less than or equal to the topic's partition count
- All consumers share the same consumer group
- Kafka will distribute partitions among consumers automatically
- Each consumer runs as a separate background service

## Message Compatibility

The Kafka extension maintains full compatibility with JustSaying's `Message` base class:

```csharp
public class OrderPlacedEvent : Message
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    
    // All Message properties are preserved:
    // - Id (Guid)
    // - TimeStamp (DateTime)
    // - RaisingComponent
    // - Tenant
    // - Conversation
}
```

### CloudEvents Mapping

JustSaying `Message` properties map to CloudEvents as follows:

| Message Property | CloudEvents Attribute | Notes |
|-----------------|----------------------|-------|
| `Id` | `id` | Message unique identifier |
| `TimeStamp` | `time` | Message creation time |
| `Type` (class name) | `type` | Full type name |
| `RaisingComponent` | `raisingcomponent` | Custom extension attribute |
| `Tenant` | `tenant` | Custom extension attribute |
| `Conversation` | `conversation` | Custom extension attribute |
| Message body | `data` | Serialized message content |

## Batch Publishing

Efficiently publish multiple messages in a batch:

```csharp
var messages = new[]
{
    new OrderPlacedEvent { OrderId = "1", Amount = 10.00m },
    new OrderPlacedEvent { OrderId = "2", Amount = 20.00m },
    new OrderPlacedEvent { OrderId = "3", Amount = 30.00m }
};

await publisher.PublishAsync(messages, new PublishBatchMetadata
{
    BatchSize = 100 // Optional batch size
});
```

## Fluent Builder API

Use the fluent builder API for more control:

```csharp
using JustSaying.Extensions.Kafka.Fluent;

var publisherBuilder = new KafkaPublisherBuilder<OrderPlacedEvent>("order-events")
    .WithBootstrapServers("localhost:9092")
    .WithCloudEvents(enable: true, source: "urn:myapp:orders")
    .WithProducerConfig(config =>
    {
        config.Acks = Acks.All;
        config.EnableIdempotence = true;
    })
    .WithProducerSetting("linger.ms", "10")
    .WithProducerSetting("batch.size", "16384");

// Build using service provider
var publisher = publisherBuilder.Build(serializationFactory, loggerFactory);
```

## Monitoring and Observability

The Kafka extension integrates with JustSaying's monitoring:

```csharp
publisher.MessageResponseLogger = (response, message) =>
{
    Console.WriteLine($"Published message {response.MessageId}");
};

publisher.MessageBatchResponseLogger = (response, messages) =>
{
    Console.WriteLine($"Published batch: {response.SuccessfulMessageIds?.Length} successful");
};
```

## Error Handling

The extension throws standard JustSaying exceptions:

```csharp
try
{
    await publisher.PublishAsync(message);
}
catch (PublishException ex)
{
    // Handle publish failure
    Console.WriteLine($"Failed to publish: {ex.Message}");
}
```

## Integration with Existing JustSaying Applications

The Kafka extension can be used alongside existing SNS/SQS publishers:

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("us-east-1"));
    
    // SNS/SQS for some messages
    config.Publications(pub =>
    {
        pub.WithTopic<LegacyEvent>();
    });
    
    // Kafka for other messages
    config.WithKafkaPublisher<NewEvent>("new-events", kafka =>
    {
        kafka.BootstrapServers = "localhost:9092";
    });
});
```

## Failure Handling & Dead Letter Topics

JustSaying.Extensions.Kafka supports two retry modes to handle message processing failures:

### In-Process Retry (Default) 💰

Retries happen within the consumer with partition pause. This is the cost-optimized default mode - only one additional topic (DLT) is needed.

```csharp
config.Subscriptions(sub =>
{
    sub.ForKafkaTopic<OrderEvent>("orders", kafka =>
    {
        kafka.WithBootstrapServers("localhost:9092")
             .WithGroupId("order-processor")
             .WithDeadLetterTopic("orders.dlt")
             .WithInProcessRetry(
                 maxAttempts: 3,
                 initialBackoff: TimeSpan.FromSeconds(5),
                 exponentialBackoff: true);
    });
});
```

**Behavior:**
- Partition is paused during backoff (other partitions continue processing)
- After max retries, message is sent to DLT
- Only 1 extra topic needed (DLT)
- Lower Confluent Cloud cost

### Topic Chaining (Optional) 💰💰💰

Retries via separate topics. Non-blocking but requires more topics (higher Confluent Cloud costs).

```csharp
config.Subscriptions(sub =>
{
    // Main topic → retry-1 on failure
    sub.ForKafkaTopic<OrderEvent>("orders", kafka =>
    {
        kafka.WithBootstrapServers("localhost:9092")
             .WithGroupId("order-processor")
             .WithTopicChainingRetry("orders.retry-1");
    });

    // Retry-1 (30s delay) → DLT on failure
    sub.ForKafkaTopic<OrderEvent>("orders.retry-1", kafka =>
    {
        kafka.WithBootstrapServers("localhost:9092")
             .WithGroupId("order-processor-retry")
             .WithProcessingDelay(TimeSpan.FromSeconds(30))
             .WithTopicChainingRetry("orders.dlt");
    });
});
```

**When to use Topic Chaining:**
- High-throughput scenarios where partition blocking is unacceptable
- When you have budget for additional Confluent Cloud topics
- Latency-sensitive applications

### No Retry - Direct to DLT

```csharp
kafka.WithNoRetry()
     .WithDeadLetterTopic("orders.dlt");
```

### Custom Failure Handler

For advanced scenarios, you can provide a custom failure handler:

```csharp
kafka.WithFailureHandler(sp => new MyCustomFailureHandler(
    sp.GetRequiredService<IAlertService>()));
```

## Typed Producers

For applications that need multiple Kafka producers with different configurations, use typed producer registration:

```csharp
// Define marker types for each producer
public class OrderServiceProducer { }
public class PaymentServiceProducer { }

// Register typed producers
services.AddKafkaProducer<OrderServiceProducer>(config =>
{
    config.BootstrapServers = "orders-cluster:9092";
    config.CloudEventsSource = "//orders/service";
});

services.AddKafkaProducer<PaymentServiceProducer>(config =>
{
    config.BootstrapServers = "payments-cluster:9092";
    config.CloudEventsSource = "//payments/service";
});

// Inject and use typed producers
public class OrderService
{
    private readonly IKafkaProducer<OrderServiceProducer> _producer;
    
    public OrderService(IKafkaProducer<OrderServiceProducer> producer)
    {
        _producer = producer;
    }
    
    public async Task PlaceOrderAsync(Order order)
    {
        var @event = new OrderPlacedEvent { OrderId = order.Id };
        await _producer.ProduceAsync("order-events", @event);
    }
}
```

### Non-blocking Produce with Callback

For high-throughput scenarios, use the non-blocking produce method:

```csharp
// Fire and forget with callback
_producer.Produce("order-events", @event, report =>
{
    if (report.Error.IsError)
        _logger.LogError("Delivery failed: {Error}", report.Error.Reason);
    else
        _logger.LogDebug("Delivered to {Topic} offset {Offset}", 
            report.Topic, report.Offset.Value);
});

// Flush before shutdown
_producer.Flush(TimeSpan.FromSeconds(10));
```

## Consumer Workers (BackgroundService)

For simpler consumer setup, use the built-in worker pattern:

```csharp
// Register handlers
services.AddSingleton<IHandlerAsync<OrderPlacedEvent>, OrderPlacedEventHandler>();

// Add consumer worker - automatically starts on host startup
services.AddKafkaConsumerWorker<OrderPlacedEvent>("order-events", config =>
{
    config.BootstrapServers = "localhost:9092";
    config.GroupId = "order-processor";
    config.NumberOfConsumers = 3;  // Runs 3 consumer instances
    
    // Configure retry
    config.Retry.Mode = RetryMode.InProcess;
    config.Retry.MaxRetryAttempts = 3;
    config.DeadLetterTopic = "order-events-dlt";
});
```

This automatically:
- Creates the specified number of consumer instances
- Starts consuming when the host starts
- Stops gracefully on shutdown
- Resolves handlers from DI

## Rich Message Context

Access detailed Kafka message metadata in handlers using `IKafkaMessageContextAccessor`:

```csharp
// Register the context accessor
services.AddKafkaMessageContextAccessor();

// Inject and use in handlers
public class OrderHandler : IHandlerAsync<OrderEvent>
{
    private readonly IKafkaMessageContextAccessor _contextAccessor;
    private readonly ILogger<OrderHandler> _logger;

    public OrderHandler(
        IKafkaMessageContextAccessor contextAccessor,
        ILogger<OrderHandler> logger)
    {
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    public Task<bool> Handle(OrderEvent message)
    {
        var ctx = _contextAccessor.Context;
        
        _logger.LogInformation(
            "Processing message from partition {Partition}, offset {Offset}, lag {Lag}ms",
            ctx.Partition,
            ctx.Offset,
            ctx.LagMilliseconds);
        
        // Access CloudEvent metadata if available
        if (ctx.CloudEventType != null)
        {
            _logger.LogInformation("CloudEvent type: {Type}", ctx.CloudEventType);
        }
        
        return Task.FromResult(true);
    }
}
```

Available context properties:
- `Topic`, `Partition`, `Offset` - Kafka coordinates
- `Key` - Partition key
- `Timestamp`, `ReceivedAt`, `LagMilliseconds` - Timing information
- `Headers` - Message headers as dictionary
- `GroupId`, `ConsumerId` - Consumer identification
- `RetryAttempt` - Current retry attempt (1-based)
- `CloudEventType`, `CloudEventSource`, `CloudEventId` - CloudEvents metadata

## Warm-up Exclusion

Kafka components are marked with `[IgnoreKafkaInWarmUp]` to exclude them from startup warm-up patterns:

```csharp
// Filter services for warm-up (in custom health checks or startup logic)
var warmUpServices = services
    .Where(d => !d.ServiceType.IsDefined(typeof(IgnoreKafkaInWarmUpAttribute), false))
    .Where(d => d.ImplementationType == null || 
                !d.ImplementationType.IsDefined(typeof(IgnoreKafkaInWarmUpAttribute), false));
```

This prevents Kafka connections from being established during application warm-up phases.

## Best Practices

1. **Use CloudEvents**: Enable CloudEvents for better interoperability and event metadata
2. **Consumer Groups**: Use meaningful consumer group IDs for horizontal scaling
3. **Error Handling**: Implement proper error handling in message handlers
4. **Idempotency**: Design handlers to be idempotent using `Message.UniqueKey()`
5. **Monitoring**: Set up logging and monitoring for message processing
6. **Testing**: Use the test helpers to verify message handling logic
7. **Dead Letter Topics**: Configure DLT for production to prevent message loss
8. **Cost Optimization**: Use in-process retry (default) to minimize Confluent Cloud costs
9. **Typed Producers**: Use marker types for multiple producer configurations
10. **Consumer Workers**: Use `AddKafkaConsumerWorker` for simple consumer setup

## Example: Complete Application

```csharp
using JustSaying;
using JustSaying.Extensions.Kafka;
using JustSaying.Extensions.Kafka.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Marker type for the order service producer
public class OrderProducer { }

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add serialization factory
                services.AddSingleton<IMessageBodySerializationFactory>(
                    new SystemTextJsonSerializationFactory(new JsonSerializerOptions()));
                
                // Add typed producer for publishing
                services.AddKafkaProducer<OrderProducer>(config =>
                {
                    config.BootstrapServers = "localhost:9092";
                    config.EnableCloudEvents = true;
                    config.CloudEventsSource = "urn:myapp:orders";
                });
                
                // Add handler
                services.AddSingleton<IHandlerAsync<OrderPlacedEvent>, OrderPlacedEventHandler>();
                
                // Add consumer worker (auto-starts with host)
                services.AddKafkaConsumerWorker<OrderPlacedEvent>("order-events", config =>
                {
                    config.BootstrapServers = "localhost:9092";
                    config.GroupId = "order-processor";
                    config.EnableCloudEvents = true;
                    config.Retry.MaxRetryAttempts = 3;
                    config.DeadLetterTopic = "order-events-dlt";
                });
                
                // Add monitoring (optional)
                services.AddKafkaLoggingMonitor();
            })
            .Build();

        await host.RunAsync();
    }
}

// Handler implementation
public class OrderPlacedEventHandler : IHandlerAsync<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedEventHandler> _logger;

    public OrderPlacedEventHandler(ILogger<OrderPlacedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> Handle(OrderPlacedEvent message)
    {
        _logger.LogInformation("Processing order {OrderId}", message.OrderId);
        // Process the order...
        return true;
    }
}
```

## Distributed Tracing

Built-in OpenTelemetry distributed tracing support using W3C Trace Context:

```csharp
// Enable distributed tracing
services.AddKafkaDistributedTracing();

// Or enable both metrics and tracing
services.AddKafkaOpenTelemetry();

// Configure OpenTelemetry to collect traces
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("JustSaying.Kafka"));
```

Trace context is automatically propagated via Kafka message headers:
- Producer injects `traceparent` and `tracestate` headers
- Consumer extracts trace context and creates linked spans
- Spans include Kafka-specific tags (topic, partition, offset, etc.)

## Advanced Partitioning Strategies

Configure custom partition key strategies for message routing:

```csharp
// Built-in strategies
builder.WithMessageIdPartitioning();           // Uses message.Id (default)
builder.WithUniqueKeyPartitioning();           // Uses message.UniqueKey()
builder.WithRoundRobinPartitioning();          // Kafka distributes evenly
builder.WithStickyPartitioning(TimeSpan.FromSeconds(5)); // Sticky to partition for duration
builder.WithTimeBasedPartitioning(TimeSpan.FromHours(1)); // Route by timestamp window

// Custom property-based partitioning
builder.WithConsistentHashPartitioning(msg => msg.CustomerId);

// Fully custom partitioning
builder.WithCustomPartitioning((msg, topic) => $"{msg.Region}:{msg.CustomerId}");
```

Available strategies:
- `MessageIdPartitionKeyStrategy` - Uses message ID
- `UniqueKeyPartitionKeyStrategy` - Uses message.UniqueKey()
- `RoundRobinPartitionKeyStrategy` - Null key for Kafka's round-robin
- `StickyPartitionKeyStrategy` - Sticks to one partition for a time period
- `TimeBasedPartitionKeyStrategy` - Routes based on message timestamp
- `ConsistentHashPartitionKeyStrategy<T>` - Hash-based on a property
- `Murmur3PartitionKeyStrategy` - Murmur3 hash for better distribution
- `DelegatePartitionKeyStrategy` - Custom delegate

## Stream Processing

Lightweight Kafka Streams-like abstractions for .NET:

```csharp
using JustSaying.Extensions.Kafka.Streams;

// Define a stream processing topology
services.AddKafkaStream<OrderEvent>("orders", stream =>
{
    stream.WithBootstrapServers("localhost:9092")
          .WithGroupId("order-processor")
          // Filter out cancelled orders
          .Filter(order => order.Status != "Cancelled")
          // Transform to shipping event
          .Map(order => new ShippingEvent 
          { 
              OrderId = order.Id, 
              Address = order.ShippingAddress 
          })
          // Add side effect logging
          .Peek(shipping => Console.WriteLine($"Shipping: {shipping.OrderId}"))
          // Send to output topic
          .To("shipping-events");
});
```

### Stream Operations

| Operation | Description |
|-----------|-------------|
| `Filter(predicate)` | Keep only messages matching the predicate |
| `Map(transform)` | Transform each message to a new type |
| `FlatMap(transform)` | Transform each message to zero or more messages |
| `Peek(action)` | Perform side effects without changing the stream |
| `Branch(predicates)` | Route messages to different topics based on conditions |
| `GroupBy(keySelector)` | Group messages by a key for aggregation |

### Windowed Aggregations

```csharp
stream.GroupBy(order => order.CustomerId)
      .WindowedBy(TimeSpan.FromMinutes(5))  // 5-minute tumbling windows
      .Count();  // Count orders per customer per window
```

Window types:
- **Tumbling**: Fixed-size, non-overlapping windows
- **Sliding**: Fixed-size, overlapping windows
- **Session**: Variable-size windows based on activity gaps

## Contributing

Contributions are welcome! Please see the [main JustSaying repository](https://github.com/justeattakeaway/JustSaying) for contribution guidelines.

## License

This project is licensed under the Apache 2.0 License - see the LICENSE file in the main JustSaying repository for details.
