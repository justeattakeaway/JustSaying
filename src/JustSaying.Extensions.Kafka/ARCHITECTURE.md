# JustSaying Kafka Transport - Architecture & Design

This document explains the architecture and design decisions for the Kafka transport extension with CloudEvents support.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  (Your existing JustSaying Message classes - no changes!)   │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│              JustSaying Core Interfaces                      │
│  - IMessagePublisher / IMessageBatchPublisher               │
│  - IHandlerAsync<T>                                         │
│  - Message (base class)                                     │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│         JustSaying.Extensions.Kafka (This Library)          │
│                                                              │
│  ┌────────────────────┐      ┌──────────────────────┐     │
│  │ KafkaMessage       │◄────►│ CloudEvents          │     │
│  │ Publisher          │      │ MessageConverter     │     │
│  └────────────────────┘      └──────────────────────┘     │
│           │                            │                    │
│           │                            │                    │
│  ┌────────▼────────────┐      ┌───────▼──────────────┐    │
│  │ KafkaMessage       │      │ CloudEvents API       │    │
│  │ Consumer           │      │ (CloudNative.         │    │
│  └────────────────────┘      │  CloudEvents)         │    │
│                              └──────────────────────┘     │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                    Confluent.Kafka                           │
│              (Apache Kafka .NET Client)                      │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                     Apache Kafka                             │
│                  (Message Broker)                            │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. CloudEventsMessageConverter

**Purpose**: Bidirectional conversion between JustSaying `Message` and CloudEvents.

**Design Decisions**:
- ✅ **Lossless Conversion**: All Message properties are preserved
- ✅ **CloudEvents Compliant**: Follows CloudEvents v1.0 specification
- ✅ **Extension Attributes**: Custom Message properties map to CloudEvents extensions
- ✅ **Type Safety**: Maintains strong typing throughout conversion

**Mapping Strategy**:
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

### 2. KafkaMessagePublisher

**Purpose**: Publishes JustSaying Messages to Kafka topics.

**Design Decisions**:
- ✅ **Dual Interface**: Implements both `IMessagePublisher` and `IMessageBatchPublisher`
- ✅ **CloudEvents Optional**: Can disable CloudEvents for backward compatibility
- ✅ **Metadata Headers**: Stores CloudEvents as message headers (structured mode)
- ✅ **Key Strategy**: Uses `Message.UniqueKey()` for Kafka message key (partitioning)

**Publishing Flow**:
```
Message → CloudEventsConverter.ToCloudEvent() 
       → CloudEventsConverter.Serialize() 
       → Kafka Headers (content-type, etc.)
       → Producer.ProduceAsync()
```

### 3. KafkaMessageConsumer

**Purpose**: Consumes messages from Kafka and dispatches to handlers.

**Design Decisions**:
- ✅ **Handler Interface**: Uses existing `IHandlerAsync<T>` interface
- ✅ **Manual Commit**: Commits after successful handling
- ✅ **Format Detection**: Auto-detects CloudEvents vs standard JSON
- ✅ **Error Resilience**: Continues consuming on handler errors

**Consumption Flow**:
```
Kafka Message → Detect Format (CloudEvents or JSON)
             → CloudEventsConverter.FromCloudEvent()
             → Handler.Handle(message)
             → Commit Offset (if handled successfully)
```

## CloudEvents Compliance

### Why CloudEvents?

1. **Standardization**: Industry-standard event format
2. **Interoperability**: Works with any CloudEvents-compatible system
3. **Tooling**: Leverages existing CloudEvents libraries and tools
4. **Future-Proof**: Vendor-neutral specification

### CloudEvents Specification Support

| Feature | Support | Notes |
|---------|---------|-------|
| Structured Content Mode | ✅ | Message body contains full CloudEvent |
| Binary Content Mode | ❌ | Not implemented (structured mode preferred) |
| Required Attributes | ✅ | id, source, specversion, type |
| Optional Attributes | ✅ | datacontenttype, subject, time |
| Extension Attributes | ✅ | Custom JustSaying metadata |
| JSON Event Format | ✅ | Default format |
| Content Type | ✅ | application/cloudevents+json |

### Example CloudEvents Message

```json
{
  "specversion": "1.0",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "type": "MyApp.Events.OrderPlacedEvent",
  "source": "urn:justsaying:sample:orders",
  "time": "2024-12-02T10:30:00.000Z",
  "datacontenttype": "application/json",
  "subject": "OrderPlacedEvent",
  "data": {
    "orderId": "ORD-12345",
    "customerId": "CUST-001",
    "amount": 99.99
  },
  "raisingcomponent": "OrderService",
  "tenant": "tenant-demo",
  "conversation": "conv-123"
}
```

## Backward Compatibility

### With Existing Messages

All existing `Message` subclasses work without modification:

```csharp
// Your existing message - no changes needed!
public class OrderPlacedEvent : Message
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
}
```

### Non-CloudEvents Mode

For systems that don't support CloudEvents:

```csharp
kafka.EnableCloudEvents = false;
```

Publishes standard JSON:
```json
{
  "orderId": "ORD-12345",
  "amount": 99.99,
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "timeStamp": "2024-12-02T10:30:00.000Z"
}
```

## Configuration Architecture

### KafkaConfiguration

Central configuration object with validation:

```csharp
public class KafkaConfiguration
{
    // Required
    public string BootstrapServers { get; set; }
    
    // Optional - defaults provided
    public bool EnableCloudEvents { get; set; } = true;
    public string CloudEventsSource { get; set; } = "urn:justsaying";
    
    // Advanced
    public ProducerConfig ProducerConfig { get; set; }
    public ConsumerConfig ConsumerConfig { get; set; }
}
```

### Fluent Builder Pattern

Type-safe configuration with fluent API:

```csharp
new KafkaPublisherBuilder<T>("topic")
    .WithBootstrapServers("localhost:9092")
    .WithCloudEvents(true, "urn:myapp")
    .WithProducerConfig(c => c.Acks = Acks.All)
    .Build();
```

## Extension Integration

### Dependency Injection

Integrates with `MessagingBusBuilder`:

```csharp
public static MessagingBusBuilder WithKafkaPublisher<T>(
    this MessagingBusBuilder builder,
    string topic,
    Action<KafkaConfiguration> configure)
{
    // Registers publisher in DI container
    // Configures with existing serialization factory
    // Maintains compatibility with other publishers
}
```

### Service Resolution

Uses existing JustSaying service resolution:
- `ILoggerFactory`: For logging
- `IMessageBodySerializationFactory`: For serialization
- `IServiceProvider`: For dependency injection

## Serialization Strategy

### Message Serialization

Reuses existing JustSaying serialization:

```csharp
IMessageBodySerializationFactory
  → GetSerializer<T>()
  → IMessageBodySerializer
  → Serialize(Message) → string
```

Supports:
- `SystemTextJsonMessageBodySerializer<T>` (default)
- `NewtonsoftMessageBodySerializer<T>`
- Custom serializers

### CloudEvents Serialization

Uses CloudNative.CloudEvents library:

```csharp
JsonEventFormatter
  → EncodeStructuredModeMessage(cloudEvent)
  → byte[] (UTF-8 JSON)
```

## Error Handling

### Publisher Errors

```csharp
try {
    await publisher.PublishAsync(message);
} catch (ProduceException<string, byte[]> ex) {
    // Kafka-specific error
    throw new PublishException("...", ex);
}
```

### Consumer Errors

```csharp
try {
    var handled = await handler.Handle(message);
    if (handled) {
        consumer.Commit(consumeResult);
    }
} catch (Exception ex) {
    // Log error, continue consuming
    // No commit - message will be reprocessed
}
```

## Performance Considerations

### Batching

Efficient batch publishing:
```csharp
await publisher.PublishAsync(messages, new PublishBatchMetadata {
    BatchSize = 100 // Configurable batch size
});
```

### Compression

Kafka-level compression:
```csharp
kafka.ProducerConfig = new ProducerConfig {
    CompressionType = CompressionType.Snappy
};
```

### Partitioning

Uses `Message.UniqueKey()` for consistent partitioning:
```csharp
var key = message.UniqueKey(); // Default: message.Id.ToString()
// Messages with same key → same partition → ordering guaranteed
```

## Testing Strategy

### Unit Tests

- CloudEvents conversion round-trip
- Configuration validation
- Serialization/deserialization

### Integration Tests

- End-to-end publish/consume
- CloudEvents compliance
- Error scenarios

### Sample Application

- Real-world usage demonstration
- Docker Compose for local testing
- Performance benchmarking

## Future Enhancements

Potential future additions:

1. **Binary Content Mode**: Support for binary CloudEvents encoding
2. **Schema Registry**: Integration with Confluent Schema Registry
3. **Kafka Streams**: Support for stream processing
4. **Dead Letter Topics**: Automatic DLT configuration
5. **Metrics**: Prometheus metrics export
6. **Tracing**: Distributed tracing support (OpenTelemetry)

## Design Principles

1. **Backward Compatibility**: Existing code continues to work
2. **CloudEvents First**: Default to standards-based approach
3. **Configuration Over Code**: Externalize all configuration
4. **Fail Fast**: Validate configuration at startup
5. **Observability**: Comprehensive logging and monitoring
6. **Type Safety**: Maintain strong typing throughout
7. **Testability**: Design for easy testing

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Confluent.Kafka | 2.6.1 | Kafka client |
| CloudNative.CloudEvents | 3.0.0 | CloudEvents core |
| CloudNative.CloudEvents.Kafka | 3.0.0 | Kafka-specific CloudEvents |
| JustSaying | (internal) | Core messaging framework |

## References

- [CloudEvents Specification v1.0](https://github.com/cloudevents/spec/blob/v1.0/spec.md)
- [Kafka Protocol Guide](https://kafka.apache.org/protocol.html)
- [Confluent .NET Client](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)
- [JustSaying Documentation](https://justeat.gitbook.io/justsaying/)
