# Kafka Transport Migration Guide

This guide helps you migrate from SNS/SQS to Kafka transport while maintaining full compatibility with your existing JustSaying `Message` types.

## Why Kafka with CloudEvents?

- **Interoperability**: CloudEvents provides a standard format for event metadata
- **Vendor Neutrality**: Not tied to AWS infrastructure
- **Stream Processing**: Better support for stream processing and event sourcing
- **Ordering Guarantees**: Kafka provides strong ordering within partitions
- **Replay Capability**: Easily replay events for debugging or reprocessing

## Key Concepts

### Message Compatibility

Your existing `Message` classes work without modification:

```csharp
// Before (works with SNS/SQS)
public class OrderPlacedEvent : Message
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
}

// After (same class works with Kafka!)
// No changes needed!
```

### CloudEvents Mapping

JustSaying automatically maps your `Message` properties to CloudEvents:

| Your Message | CloudEvents | Example |
|--------------|-------------|---------|
| `message.Id` | `id` | "3c8f5c4e-7b2a-4d9e-8f1a-2b3c4d5e6f7a" |
| `message.GetType().FullName` | `type` | "MyApp.Events.OrderPlacedEvent" |
| `message.TimeStamp` | `time` | "2024-12-02T10:30:00Z" |
| `message.RaisingComponent` | `raisingcomponent` (extension) | "OrderService" |
| `message.Tenant` | `tenant` (extension) | "tenant-1" |
| Message body | `data` | Your serialized message |

## Migration Steps

### Step 1: Add NuGet Package

```bash
dotnet add package JustSaying.Extensions.Kafka
```

### Step 2: Update Publisher Configuration

**Before (SNS/SQS):**
```csharp
services.AddJustSaying(config =>
{
    config.Publications(pub =>
    {
        pub.WithTopic<OrderPlacedEvent>();
    });
});
```

**After (Kafka with CloudEvents):**
```csharp
services.AddJustSaying(config =>
{
    config.WithKafkaPublisher<OrderPlacedEvent>("order-events", kafka =>
    {
        kafka.BootstrapServers = "localhost:9092";
        kafka.EnableCloudEvents = true; // Default
        kafka.CloudEventsSource = "urn:myapp:orders";
    });
});
```

### Step 3: Update Consumer Configuration

**Before (SQS):**
```csharp
config.Subscriptions(sub =>
{
    sub.ForQueue<OrderPlacedEvent>("order-queue");
});
```

**After (Kafka with CloudEvents):**
```csharp
// In your background service or consumer
var consumer = serviceProvider.CreateKafkaConsumer("order-events", kafka =>
{
    kafka.BootstrapServers = "localhost:9092";
    kafka.GroupId = "order-processor";
    kafka.EnableCloudEvents = true;
});

await consumer.StartAsync(handler, cancellationToken);
```

## Side-by-Side Comparison

### Publishing

| Feature | SNS/SQS | Kafka with CloudEvents |
|---------|---------|----------------------|
| Message Type | Same `Message` class | Same `Message` class ✅ |
| API | `PublishAsync(message)` | `PublishAsync(message)` ✅ |
| Batch Publishing | ✅ | ✅ |
| Metadata | Message attributes | CloudEvents attributes + extensions |
| Format | JSON | CloudEvents JSON (spec compliant) |

### Consuming

| Feature | SNS/SQS | Kafka with CloudEvents |
|---------|---------|----------------------|
| Handler Interface | `IHandlerAsync<T>` | `IHandlerAsync<T>` ✅ |
| Message Type | Same `Message` class | Same `Message` class ✅ |
| Acknowledgment | Automatic | Manual commit after handler returns true |
| Retry | DLQ | Kafka consumer retry logic |

## Dual Transport Support

You can run both transports simultaneously during migration:

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("us-east-1"));
    
    // Keep existing SNS/SQS for legacy messages
    config.Publications(pub =>
    {
        pub.WithTopic<LegacyEvent>();
    });
    
    config.Subscriptions(sub =>
    {
        sub.ForQueue<LegacyEvent>("legacy-queue");
    });
    
    // Add Kafka for new messages
    config.WithKafkaPublisher<NewEvent>("new-events", kafka =>
    {
        kafka.BootstrapServers = "localhost:9092";
        kafka.EnableCloudEvents = true;
    });
});
```

## CloudEvents vs Standard JSON

### With CloudEvents (Recommended)

```csharp
kafka.EnableCloudEvents = true; // Default
```

**Published message:**
```json
{
  "specversion": "1.0",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "type": "MyApp.OrderPlacedEvent",
  "source": "urn:myapp:orders",
  "time": "2024-12-02T10:30:00Z",
  "datacontenttype": "application/json",
  "data": {
    "orderId": "12345",
    "amount": 99.99
  }
}
```

### Without CloudEvents (Backward Compatibility)

```csharp
kafka.EnableCloudEvents = false;
```

**Published message:**
```json
{
  "orderId": "12345",
  "amount": 99.99,
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "timeStamp": "2024-12-02T10:30:00Z"
}
```

## Common Migration Patterns

### Pattern 1: Gradual Migration

Migrate message type by message type:

```csharp
// Week 1: OrderPlacedEvent
config.WithKafkaPublisher<OrderPlacedEvent>("orders", kafka => ...);

// Week 2: OrderConfirmedEvent
config.WithKafkaPublisher<OrderConfirmedEvent>("orders", kafka => ...);

// Keep legacy events on SNS/SQS
config.Publications(pub => pub.WithTopic<LegacyEvent>());
```

### Pattern 2: Dual Publishing

Publish to both during transition:

```csharp
public class DualPublisher
{
    private readonly IMessagePublisher _snsPublisher;
    private readonly IMessagePublisher _kafkaPublisher;
    
    public async Task PublishAsync<T>(T message) where T : Message
    {
        // Publish to SNS (legacy)
        await _snsPublisher.PublishAsync(message);
        
        // Publish to Kafka (new)
        await _kafkaPublisher.PublishAsync(message);
    }
}
```

### Pattern 3: Topic Mapping

Map SNS topics to Kafka topics:

```csharp
var topicMapping = new Dictionary<Type, string>
{
    [typeof(OrderPlacedEvent)] = "order-placed",
    [typeof(OrderShippedEvent)] = "order-shipped",
    [typeof(OrderDeliveredEvent)] = "order-delivered"
};

foreach (var (messageType, topic) in topicMapping)
{
    // Configure Kafka publisher dynamically
}
```

## Testing Your Migration

### Unit Testing

```csharp
[Fact]
public async Task CloudEvents_RoundTrip_PreservesMessageData()
{
    // Arrange
    var original = new OrderPlacedEvent
    {
        OrderId = "12345",
        Amount = 99.99m,
        RaisingComponent = "OrderService"
    };
    
    // Act
    var cloudEvent = converter.ToCloudEvent(original);
    var restored = converter.FromCloudEvent(cloudEvent);
    
    // Assert
    Assert.Equal(original.OrderId, ((OrderPlacedEvent)restored).OrderId);
    Assert.Equal(original.Amount, ((OrderPlacedEvent)restored).Amount);
    Assert.Equal(original.RaisingComponent, restored.RaisingComponent);
}
```

### Integration Testing

```csharp
[Fact]
public async Task KafkaPublisher_PublishAndConsume_Success()
{
    // Arrange
    var message = new OrderPlacedEvent { OrderId = "12345" };
    var received = new TaskCompletionSource<OrderPlacedEvent>();
    
    var handler = new TestHandler<OrderPlacedEvent>(m =>
    {
        received.SetResult(m);
        return Task.FromResult(true);
    });
    
    // Act
    await publisher.PublishAsync(message);
    var result = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
    
    // Assert
    Assert.Equal(message.OrderId, result.OrderId);
}
```

## Monitoring and Observability

### CloudEvents Benefits for Monitoring

CloudEvents provides standardized attributes for monitoring:

```csharp
// All CloudEvents have these standard attributes
- id: Unique event identifier
- source: Where the event originated
- type: What kind of event
- time: When it happened
- datacontenttype: Format of the data

// Your monitoring tools can use these consistently
```

### Logging

```csharp
publisher.MessageResponseLogger = (response, message) =>
{
    logger.LogInformation(
        "Published CloudEvent: id={EventId}, type={EventType}, source={Source}",
        message.Id,
        message.GetType().FullName,
        configuration.CloudEventsSource);
};
```

## Performance Considerations

### Kafka vs SNS/SQS

| Aspect | SNS/SQS | Kafka |
|--------|---------|-------|
| Throughput | Good | Excellent |
| Latency | ~100ms | ~5ms |
| Ordering | No guarantee | Per-partition ordering |
| Retention | Limited (14 days max) | Configurable (unlimited) |
| Cost | Per request | Per broker |

### Optimization Tips

1. **Batch Publishing**: Use batch APIs for better throughput
2. **Compression**: Enable compression for large messages
3. **Partitioning**: Use message keys for related events
4. **Consumer Groups**: Scale horizontally with multiple consumers

## Troubleshooting

### Common Issues

**Issue**: Messages not appearing in Kafka
```csharp
// Check Kafka connection
kafka.BootstrapServers = "localhost:9092"; // Verify this is correct

// Enable detailed logging
services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
```

**Issue**: CloudEvents deserialization fails
```csharp
// Ensure both publisher and consumer use same CloudEvents setting
kafka.EnableCloudEvents = true; // Must match on both sides
```

**Issue**: Message metadata lost
```csharp
// CloudEvents preserves all Message properties automatically
// Check that you're using the CloudEventsMessageConverter
```

## Best Practices

1. ✅ **Use CloudEvents**: Enables interoperability and future-proofing
2. ✅ **Version Your Events**: Include version in message type name
3. ✅ **Design for Idempotency**: Use `message.UniqueKey()` for deduplication
4. ✅ **Monitor CloudEvents Attributes**: Track `source`, `type`, and `time`
5. ✅ **Test Both Formats**: Verify CloudEvents and standard JSON work
6. ✅ **Document Your Sources**: Maintain a registry of CloudEvents sources

## Next Steps

1. Review the [README](README.md) for detailed API documentation
2. Run the [sample application](../../samples/src/JustSaying.Sample.Kafka/) locally
3. Set up Kafka cluster (or use Docker for local development)
4. Start with one message type for initial migration
5. Monitor and validate before expanding to more message types

## Getting Help

- [CloudEvents Specification](https://cloudevents.io/)
- [JustSaying Documentation](https://justeat.gitbook.io/justsaying/)
- [Confluent Kafka .NET Client](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)
