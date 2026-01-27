# JustSaying Kafka Transport - Architecture & Design

This document explains the architecture and design decisions for the Kafka transport extension with CloudEvents support, Dead Letter Topics, distributed tracing, and stream processing capabilities.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Application Layer                                  │
│          (Your existing JustSaying Message classes - no changes!)           │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      JustSaying Core Interfaces                              │
│  - IMessagePublisher / IMessageBatchPublisher                               │
│  - IHandlerAsync<T>                                                         │
│  - Message (base class)                                                     │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│              JustSaying.Extensions.Kafka (This Library)                      │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                       Messaging Layer                                 │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐  │  │
│  │  │ KafkaMessage    │  │ KafkaMessage    │  │ KafkaProducer<T>    │  │  │
│  │  │ Publisher       │  │ Consumer        │  │ (Typed Producer)    │  │  │
│  │  └────────┬────────┘  └────────┬────────┘  └─────────────────────┘  │  │
│  └───────────┼─────────────────────┼────────────────────────────────────┘  │
│              │                     │                                        │
│  ┌───────────┼─────────────────────┼────────────────────────────────────┐  │
│  │           │    Error Handling & Resilience                            │  │
│  │  ┌────────▼────────┐  ┌────────▼────────┐  ┌─────────────────────┐  │  │
│  │  │ In-Process      │  │ Topic Chaining  │  │ Dead Letter Topic   │  │  │
│  │  │ Retry Handler   │  │ Retry Handler   │  │ (DLT) Publisher     │  │  │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    Observability Layer                                │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐  │  │
│  │  │ IKafkaConsumer  │  │ OpenTelemetry   │  │ Distributed         │  │  │
│  │  │ Monitor         │  │ Metrics         │  │ Tracing             │  │  │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    Stream Processing Layer                            │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐  │  │
│  │  │ KafkaStream     │  │ Stream          │  │ Windowed            │  │  │
│  │  │ Builder         │  │ Processors      │  │ Aggregations        │  │  │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    Infrastructure Layer                               │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐  │  │
│  │  │ CloudEvents     │  │ Kafka           │  │ Partition Key       │  │  │
│  │  │ Converter       │  │ Factories       │  │ Strategies          │  │  │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Confluent.Kafka                                     │
│                    (Apache Kafka .NET Client)                                │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Apache Kafka                                       │
│                        (Message Broker)                                      │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Component Architecture

### Core Messaging Components

#### 1. KafkaMessagePublisher

**Purpose**: Publishes JustSaying Messages to Kafka topics.

**Features**:
- Dual interface support (`IMessagePublisher` and `IMessageBatchPublisher`)
- CloudEvents serialization (optional)
- Configurable partition key strategies
- Distributed tracing context propagation
- Async batch publishing

```
Message → PartitionKeyStrategy.GetKey()
       → CloudEventsConverter.ToCloudEvent() 
       → TraceContextPropagator.Inject()
       → Producer.ProduceAsync()
```

#### 2. KafkaMessageConsumer

**Purpose**: Consumes messages from Kafka and dispatches to handlers.

**Features**:
- Automatic format detection (CloudEvents vs JSON)
- Configurable retry mechanisms (In-Process or Topic Chaining)
- Dead Letter Topic support
- Partition rebalance handling
- Consumer monitoring hooks
- Trace context extraction

```
Kafka Message → TraceContextPropagator.Extract()
             → Detect Format
             → CloudEventsConverter.FromCloudEvent()
             → Monitor.OnMessageReceived()
             → RetryHandler.Execute(Handler.Handle())
             → Monitor.OnMessageProcessed() or OnMessageFailed()
             → Commit or SendToDLT
```

#### 3. KafkaProducer<T> (Typed Producer)

**Purpose**: Type-safe producer for scenarios requiring multiple producer configurations.

```csharp
public interface IKafkaProducer<TProducerType>
{
    Task<bool> ProduceAsync<T>(string topic, T message, string key = null, 
        CancellationToken ct = default) where T : Message;
    void Produce<T>(string topic, T message, string key = null, 
        Action<DeliveryResult<string, byte[]>> deliveryHandler = null) where T : Message;
    void Flush(TimeSpan timeout);
}
```

### Error Handling & Resilience

#### Dead Letter Topic (DLT) Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                    Message Processing Flow                      │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│   Main Topic                                                   │
│   ┌─────────┐                                                  │
│   │ Message │────────────────────────────────────────┐        │
│   └─────────┘                                         │        │
│        │                                              │        │
│        ▼                                              │        │
│   ┌─────────────────────────────────────────────┐    │        │
│   │           Handler Processing                 │    │        │
│   │  ┌─────────────────────────────────────┐   │    │        │
│   │  │ Attempt 1 → Failed                   │   │    │        │
│   │  │ Attempt 2 → Failed (backoff: 1s)     │   │    │        │
│   │  │ Attempt 3 → Failed (backoff: 2s)     │   │    │        │
│   │  │ Attempt 4 → Failed (backoff: 4s)     │   │    │        │
│   │  └─────────────────────────────────────┘   │    │        │
│   └─────────────────────────────────────────────┘    │        │
│        │                                              │        │
│        │ All retries exhausted                        │        │
│        ▼                                              │        │
│   ┌─────────────────┐                                │        │
│   │ Dead Letter     │◄───────────────────────────────┘        │
│   │ Topic (DLT)     │                                          │
│   └─────────────────┘                                          │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

#### Retry Strategies

**1. In-Process Retry (Cost-Optimized)**
- Pauses partition during backoff
- No additional topics required
- Preserves message ordering
- Best for: Cost-sensitive environments, Confluent Cloud

```csharp
kafka.WithInProcessRetry(
    maxAttempts: 3,
    initialBackoff: TimeSpan.FromSeconds(1),
    exponentialBackoff: true,
    maxBackoff: TimeSpan.FromSeconds(30));
```

**2. Topic Chaining Retry (Throughput-Optimized)**
- Uses separate retry topics
- Non-blocking retries
- Higher throughput
- Best for: High-volume processing

```csharp
kafka.WithTopicChainingRetry(
    maxAttempts: 3,
    initialBackoff: TimeSpan.FromSeconds(5))
.WithRetryTopic("orders-retry")
.WithDeadLetterTopic("orders-dlt");
```

### Observability Layer

#### Consumer Monitoring Architecture

```csharp
public interface IKafkaConsumerMonitor
{
    void OnMessageReceived<T>(MessageReceivedContext<T> context);
    void OnMessageProcessed<T>(MessageProcessedContext<T> context);
    void OnMessageFailed<T>(MessageFailedContext<T> context);
    void OnMessageDeadLettered<T>(MessageDeadLetteredContext<T> context);
}
```

**Built-in Monitors**:

| Monitor | Purpose |
|---------|---------|
| `LoggingKafkaConsumerMonitor` | Structured logging of all events |
| `OpenTelemetryKafkaConsumerMonitor` | Metrics (counters, histograms) |
| `TracingKafkaConsumerMonitor` | Distributed tracing spans |
| `CompositeKafkaConsumerMonitor` | Aggregates multiple monitors |
| `NullKafkaConsumerMonitor` | No-op for testing |

#### OpenTelemetry Metrics

Exposed under meter name: `JustSaying.Kafka`

| Metric | Type | Description |
|--------|------|-------------|
| `kafka.consumer.messages_received` | Counter | Total messages received |
| `kafka.consumer.messages_processed` | Counter | Successfully processed messages |
| `kafka.consumer.messages_failed` | Counter | Failed message processing attempts |
| `kafka.consumer.messages_dead_lettered` | Counter | Messages sent to DLT |
| `kafka.consumer.processing_duration` | Histogram | Processing time distribution |
| `kafka.consumer.message_lag` | Histogram | Consumer lag (receive time - produce time) |
| `kafka.consumer.retry_attempts` | Counter | Total retry attempts |

#### Distributed Tracing

Uses W3C Trace Context for propagation:

```
Producer                          Kafka                           Consumer
   │                                │                                 │
   │  Activity: kafka.produce       │                                 │
   │  ┌─────────────────────────┐   │                                 │
   │  │ TraceId: abc123         │   │                                 │
   │  │ SpanId: span1           │   │                                 │
   │  └───────────┬─────────────┘   │                                 │
   │              │                 │                                 │
   │              │ Inject headers  │                                 │
   │              │ (traceparent)   │                                 │
   │              ▼                 │                                 │
   │         ┌─────────────────┐    │                                 │
   │         │ Message +       │────┼────────────────────────────────►│
   │         │ traceparent     │    │                                 │
   │         └─────────────────┘    │     Activity: kafka.consume     │
   │                                │     ┌─────────────────────────┐ │
   │                                │     │ TraceId: abc123         │ │
   │                                │     │ ParentSpanId: span1     │ │
   │                                │     │ SpanId: span2           │ │
   │                                │     └─────────────────────────┘ │
```

### Stream Processing Layer

#### Stream Builder Architecture

```csharp
// Fluent stream processing pipeline
var handler = new KafkaStreamBuilder<OrderEvent>("orders")
    .WithBootstrapServers("localhost:9092")
    .WithGroupId("order-processor")
    .Filter(o => o.Status != "Cancelled")
    .Map(o => new ShippingEvent { OrderId = o.Id })
    .To("shipping-events")
    .Build();
```

**Supported Operations**:

| Operation | Description |
|-----------|-------------|
| `Filter` | Remove messages not matching predicate |
| `Map` | Transform message to different type |
| `FlatMap` | Transform message to multiple messages |
| `Peek` | Side-effect without transformation |
| `PeekAsync` | Async side-effect |
| `Branch` | Route messages to different topics |
| `GroupBy` | Group by key for aggregation |
| `WindowedBy` | Time-based windowing (Tumbling/Sliding/Session) |

#### Windowing Types

```
Tumbling Window (5 min)
├────────┤├────────┤├────────┤
|  W1    ||  W2    ||  W3    |
└────────┘└────────┘└────────┘

Sliding Window (5 min window, 1 min advance)
├────────┤
   ├────────┤
      ├────────┤
         ├────────┤

Session Window (30 min gap)
├──────┤      ├─────────────┤    ├───┤
| S1   |      |     S2      |    |S3 |
└──────┘      └─────────────┘    └───┘
     gap           gap        gap
```

### Infrastructure Layer

#### Factory Pattern

Enables testability and custom client configurations:

```csharp
public interface IKafkaConsumerFactory
{
    IConsumer<string, byte[]> CreateConsumer(ConsumerConfig config);
}

public interface IKafkaProducerFactory
{
    IProducer<string, byte[]> CreateProducer(ProducerConfig config);
}
```

#### Partition Key Strategies

| Strategy | Behavior |
|----------|----------|
| `MessageIdPartitionKeyStrategy` | Uses `Message.Id` |
| `UniqueKeyPartitionKeyStrategy` | Uses `Message.UniqueKey()` |
| `RoundRobinPartitionKeyStrategy` | Returns null (round-robin) |
| `StickyPartitionKeyStrategy` | Same key for duration |
| `TimeBasedPartitionKeyStrategy` | Time window-based key |
| `ConsistentHashPartitionKeyStrategy<T>` | Property-based consistent hashing |
| `Murmur3PartitionKeyStrategy` | Murmur3 hash of property |
| `DelegatePartitionKeyStrategy` | Custom delegate |

```csharp
// Ensure all orders for a customer go to same partition
kafka.WithConsistentHashPartitioning<OrderEvent>(o => o.CustomerId);
```

#### CloudEvents Message Conversion

```csharp
Message Property      →  CloudEvent Attribute
─────────────────────    ──────────────────────
Id (Guid)            →  id (string)
GetType().FullName   →  type
TimeStamp            →  time
Message body         →  data
RaisingComponent     →  raisingcomponent (extension)
Tenant               →  tenant (extension)
Conversation         →  conversation (extension)
```

## Configuration Architecture

### Layered Configuration

```
┌─────────────────────────────────────────────────────────────────┐
│                     Global Kafka Configuration                   │
│  config.Messaging(x => x.WithKafka(kafka => { ... }))           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ Inherited by
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              Topic-Specific Overrides                            │
│  x.ForKafka<T>("topic", kafka => { ... })                       │
└─────────────────────────────────────────────────────────────────┘
```

### KafkaConfiguration Properties

```csharp
public class KafkaConfiguration
{
    // Connection
    public string BootstrapServers { get; set; }
    public string GroupId { get; set; }
    
    // CloudEvents
    public bool EnableCloudEvents { get; set; } = true;
    public string CloudEventsSource { get; set; } = "urn:justsaying";
    
    // Retry & DLT
    public RetryConfiguration Retry { get; set; }
    public string DeadLetterTopic { get; set; }
    public string RetryTopic { get; set; }
    
    // Partitioning
    public IPartitionKeyStrategy PartitionKeyStrategy { get; set; }
    
    // Scaling
    public int NumberOfConsumers { get; set; } = 1;
    
    // Advanced
    public ProducerConfig ProducerConfig { get; set; }
    public ConsumerConfig ConsumerConfig { get; set; }
}
```

## Service Registration

### Dependency Injection Extensions

```csharp
services.AddJustSaying(config => { ... })
    
    // Add Kafka factories (for custom consumer/producer creation)
    .AddKafkaFactories()
    
    // Add typed producers
    .AddKafkaProducer<OrderProducer>(configure => { ... })
    
    // Add consumer workers as hosted services
    .AddKafkaConsumerWorker<OrderEvent>("orders", configure => { ... })
    
    // Add OpenTelemetry metrics
    .AddKafkaOpenTelemetryMetrics()
    
    // Add distributed tracing
    .AddKafkaDistributedTracing()
    
    // Add both metrics and tracing
    .AddKafkaOpenTelemetry()
    
    // Add consumer monitors
    .AddKafkaConsumerMonitor<CustomMonitor>()
    
    // Add stream processor
    .AddKafkaStream<OrderEvent>("orders", builder => { ... });
```

## Error Handling Patterns

### Publisher Error Handling

```csharp
try {
    await publisher.PublishAsync(message);
} catch (ProduceException<string, byte[]> ex) {
    // Kafka-specific error (broker down, auth failure, etc.)
    logger.LogError(ex, "Failed to publish message");
    throw new PublishException("...", ex);
}
```

### Consumer Error Handling with DLT

```csharp
// Automatic handling via configuration
kafka.WithInProcessRetry(maxAttempts: 3)
     .WithDeadLetterTopic("orders-dlt");

// Handler just needs to return false or throw
public async Task<bool> Handle(OrderEvent message)
{
    if (!CanProcess(message))
        return false;  // Triggers retry
    
    if (IsPermanentFailure(message))
        throw new InvalidOperationException("Cannot process");  // Triggers retry/DLT
    
    await ProcessOrder(message);
    return true;  // Success - commits offset
}
```

## Performance Considerations

### Batching

```csharp
// Efficient batch publishing
await publisher.PublishAsync(messages, new PublishBatchMetadata {
    BatchSize = 100
});

// Producer batching configuration
kafka.ProducerConfig = new ProducerConfig {
    LingerMs = 5,      // Batch for 5ms
    BatchSize = 16384  // 16KB batches
};
```

### Compression

```csharp
kafka.ProducerConfig = new ProducerConfig {
    CompressionType = CompressionType.Snappy
};
```

### Parallel Consumers

```csharp
// Run multiple consumers for a topic
kafka.WithNumberOfConsumers(3);
```

### Partition Assignment

```csharp
// Control partition assignment
kafka.ConsumerConfig = new ConsumerConfig {
    PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
};
```

## Testing Architecture

### Unit Testing with Factories

```csharp
var mockConsumer = Substitute.For<IConsumer<string, byte[]>>();
var factory = Substitute.For<IKafkaConsumerFactory>();
factory.CreateConsumer(Arg.Any<ConsumerConfig>()).Returns(mockConsumer);

services.AddSingleton(factory);
```

### Integration Testing

```csharp
[Fact]
public async Task PublishAndConsume_RoundTrip()
{
    // Use Testcontainers or embedded Kafka
    await using var kafka = new KafkaFixture();
    
    var publisher = CreatePublisher(kafka.BootstrapServers);
    var consumer = CreateConsumer(kafka.BootstrapServers);
    
    await publisher.PublishAsync(new OrderEvent { Id = "123" });
    
    var received = await consumer.ConsumeAsync(TimeSpan.FromSeconds(5));
    received.Id.ShouldBe("123");
}
```

## Design Principles

1. **Backward Compatibility**: Existing code continues to work
2. **CloudEvents First**: Default to standards-based approach
3. **Configuration Over Code**: Externalize all configuration
4. **Fail Fast**: Validate configuration at startup
5. **Observability**: Comprehensive metrics, logging, and tracing
6. **Type Safety**: Maintain strong typing throughout
7. **Testability**: Design for easy unit and integration testing
8. **Cost-Awareness**: Default to cost-optimized patterns (e.g., in-process retry)
9. **Extensibility**: Plugin architecture for monitors, strategies, etc.

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Confluent.Kafka | 2.6.1 | Kafka client |
| CloudNative.CloudEvents | 2.8.0 | CloudEvents core |
| CloudNative.CloudEvents.Kafka | 2.8.0 | Kafka-specific CloudEvents |
| CloudNative.CloudEvents.SystemTextJson | 2.8.0 | JSON serialization |
| System.Diagnostics.DiagnosticSource | 8.0.0 | Metrics and tracing |
| Microsoft.Extensions.Hosting.Abstractions | 8.0.0 | BackgroundService support |
| JustSaying | (internal) | Core messaging framework |

## References

- [CloudEvents Specification v1.0](https://github.com/cloudevents/spec/blob/v1.0/spec.md)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/reference/specification/trace/semantic_conventions/messaging/)
- [Kafka Protocol Guide](https://kafka.apache.org/protocol.html)
- [Confluent .NET Client](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)
- [JustSaying Documentation](https://justeat.gitbook.io/justsaying/)
