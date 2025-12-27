# JustSaying Kafka Sample

This sample demonstrates how to use the JustSaying Kafka extension to publish and consume messages using Apache Kafka with CloudEvents support.

## Overview

This sample shows:
- ✅ Publishing messages to Kafka topics using the unified JustSaying API
- ✅ Consuming messages from Kafka topics with automatic handler resolution
- ✅ CloudEvents v1.0 format support for interoperability
- ✅ Side-by-side usage with AWS SQS/SNS (same API patterns)
- ✅ Message handler registration and middleware pipeline integration

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Sample Application                       │
│                                                              │
│  ┌──────────────────┐         ┌─────────────────────────┐   │
│  │ Message          │         │ Message Handlers        │   │
│  │ Generator        │         │                         │   │
│  │ Service          │         │ - OrderPlacedHandler    │   │
│  │                  │         │ - OrderConfirmedHandler │   │
│  └────────┬─────────┘         └──────────▲──────────────┘   │
│           │                              │                  │
│           │ Publish                      │ Handle           │
│           ▼                              │                  │
│  ┌────────────────────────────────────────────────┐         │
│  │        JustSaying Bus (Unified API)           │         │
│  │  ┌──────────────┐    ┌──────────────────┐     │         │
│  │  │ Publishers   │    │ Subscriptions    │     │         │
│  │  │ - Kafka      │    │ - Kafka          │     │         │
│  │  │ - (AWS SNS)  │    │ - (AWS SQS)      │     │         │
│  │  └──────────────┘    └──────────────────┘     │         │
│  └────────┬────────────────────────▲──────────────┘         │
│           │                        │                        │
└───────────┼────────────────────────┼────────────────────────┘
            │                        │
            │ CloudEvents            │ CloudEvents
            ▼                        │
   ┌────────────────────────────────────────┐
   │          Apache Kafka                  │
   │                                        │
   │  Topics:                               │
   │  - order-placed                        │
   │  - order-confirmed                     │
   └────────────────────────────────────────┘
```

## Quick Start

See [QUICKSTART.md](QUICKSTART.md) for a step-by-step guide.

### 1. Start Kafka

```bash
docker-compose up -d
```

### 2. Create Kafka Topics

The topics need to be created before the application can use them:

```bash
# Create order-placed topic
docker exec -it justsaying-kafka kafka-topics --create \
  --topic order-placed \
  --bootstrap-server localhost:9092 \
  --partitions 3 \
  --replication-factor 1

# Create order-confirmed topic
docker exec -it justsaying-kafka kafka-topics --create \
  --topic order-confirmed \
  --bootstrap-server localhost:9092 \
  --partitions 3 \
  --replication-factor 1

# Verify topics were created
docker exec -it justsaying-kafka kafka-topics --list \
  --bootstrap-server localhost:9092
```

### 3. Run the Sample

```bash
dotnet run
```

The API will be available at `http://localhost:5000` with Swagger UI at the root URL.

## Project Structure

```
JustSaying.Sample.Kafka/
├── Handlers/
│   └── OrderEventHandlers.cs       # Message handlers
├── Messages/
│   └── OrderEvents.cs               # Message definitions
├── Services/
│   └── MessageGeneratorService.cs   # Background service to publish messages
├── Program.cs                       # Application entry point and configuration
├── docker-compose.yml               # Kafka infrastructure
├── QUICKSTART.md                    # Quick start guide
└── README.md                        # This file
```

## Configuration Explained

### Global Kafka Configuration (Recommended)

Configure Kafka settings once at the messaging level, and they'll be inherited by all publications and subscriptions:

```csharp
services.AddJustSaying(builder =>
{
    builder.Messaging(config =>
    {
        config.WithRegion("us-east-1"); // Required for AWS compatibility
        
        // Set global Kafka defaults
        config.WithKafka(kafka =>
        {
            kafka.BootstrapServers = "localhost:9092";
            kafka.EnableCloudEvents = true;
            kafka.CloudEventsSource = "urn:myapp";
        });
    });

    // Publications inherit global settings - just specify topics
    builder.Publications(pubs =>
    {
        pubs.WithKafka<OrderPlacedEvent>("order-placed");
        pubs.WithKafka<OrderConfirmedEvent>("order-confirmed");
    });

    // Subscriptions inherit global settings - add consumer group
    builder.Subscriptions(subs =>
    {
        subs.ForKafka<OrderPlacedEvent>("order-placed", kafka =>
        {
            kafka.WithGroupId("my-consumer-group");
        });
    });
});
```

### Per-Topic Configuration (Override Global)

You can override global settings for specific topics:

```csharp
builder.Publications(pubs =>
{
    // Uses global settings
    pubs.WithKafka<OrderPlacedEvent>("order-placed");
    
    // Overrides bootstrap servers for this topic only
    pubs.WithKafka<OrderConfirmedEvent>("order-confirmed", kafka =>
    {
        kafka.WithBootstrapServers("different-kafka:9092")
             .WithCloudEvents(false); // Disable CloudEvents for this topic
    });
});
```

### Message Definitions

Messages inherit from `JustSaying.Models.Message`:

```csharp
public class OrderPlacedEvent : Message
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; }
    public List<OrderItem> Items { get; set; }
}
```

### Handler Registration

Handlers implement `IHandlerAsync<T>`:

```csharp
services.AddJustSayingHandler<OrderPlacedEvent, OrderPlacedEventHandler>();
```

### Publication Configuration

Configure Kafka publishers using `WithKafka<T>()`:

```csharp
builder.Publications(pubs =>
{
    pubs.WithKafka<OrderPlacedEvent>("order-placed", kafka =>
    {
        kafka.WithBootstrapServers("localhost:9092")
             .WithCloudEvents(true, "urn:justsaying:sample:orders");
    });
});
```

### Subscription Configuration

Configure Kafka subscriptions using `ForKafka<T>()`:

```csharp
builder.Subscriptions(subs =>
{
    subs.ForKafka<OrderPlacedEvent>("order-placed", kafka =>
    {
        kafka.WithBootstrapServers("localhost:9092")
             .WithGroupId("sample-consumer-group")
             .WithCloudEvents(true, "urn:justsaying:sample:orders");
    });
});
```

## CloudEvents Support

Messages are automatically wrapped in CloudEvents v1.0 format when published to Kafka:

```json
{
  "specversion": "1.0",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "type": "JustSaying.Sample.Kafka.Messages.OrderPlacedEvent",
  "source": "urn:justsaying:sample:orders",
  "time": "2025-12-02T10:30:00Z",
  "datacontenttype": "application/json",
  "subject": "OrderPlacedEvent",
  "data": {
    "orderId": "ORD-00001",
    "customerId": "CUST-042",
    "amount": 125.00,
    "orderDate": "2025-12-02T10:30:00Z",
    "items": [...]
  },
  "raisingcomponent": "OrderService",
  "tenant": "sample-tenant"
}
```

### CloudEvents Attributes

JustSaying metadata is mapped to CloudEvents attributes:
- `Message.Id` → `id`
- `Message.TimeStamp` → `time`
- Type name → `type`
- `Message.RaisingComponent` → `raisingcomponent` (extension)
- `Message.Tenant` → `tenant` (extension)

## Key Features

### 1. Unified API
Use the same API patterns as AWS SQS/SNS:
- `ForKafka<T>()` is like `ForQueue<T>()`
- `WithKafka<T>()` is like `WithTopic<T>()`

### 2. Multi-Transport Support
Mix Kafka and AWS in the same application:

```csharp
builder.Publications(pubs =>
{
    pubs.WithKafka<OrderPlacedEvent>("order-placed", kafka => { });
    pubs.WithTopic<OrderConfirmedEvent>(sns => { });
});
```

### 3. Middleware Pipeline
Full middleware support just like SQS/SNS:
- Error handling
- Logging
- Metrics
- Custom middleware

### 4. Handler Resolution
Automatic handler discovery and resolution through dependency injection.

## Monitoring

### Kafka UI
Access Kafka UI at http://localhost:8080 to:
- Browse topics and messages
- View consumer groups and offsets
- Inspect CloudEvents message structure
- Monitor throughput and lag

### Application Logs
The sample uses structured logging to show:
- Message publication events
- Message consumption events
- Handler execution results

## Customization

### Custom Producer Configuration

```csharp
kafka.WithProducerConfig(config =>
{
    config.Acks = Acks.All;
    config.EnableIdempotence = true;
    config.MaxInFlight = 5;
});
```

### Custom Consumer Configuration

```csharp
kafka.WithConsumerConfig(config =>
{
    config.AutoOffsetReset = AutoOffsetReset.Earliest;
    config.EnableAutoCommit = false;
    config.SessionTimeoutMs = 30000;
});
```

### Without CloudEvents

If you prefer plain JSON messages without CloudEvents wrapping:

```csharp
kafka.WithCloudEvents(false);
```

## Common Scenarios

### Publishing Only
Remove the subscriptions section if you only want to publish:

```csharp
services.AddJustSaying(builder =>
{
    builder.Publications(pubs =>
    {
        pubs.WithKafka<OrderPlacedEvent>("order-placed", kafka => { });
    });
});
```

### Consuming Only
Remove the publications section and message generator service:

```csharp
services.AddJustSaying(builder =>
{
    builder.Subscriptions(subs =>
    {
        subs.ForKafka<OrderPlacedEvent>("order-placed", kafka => { });
    });
});
```

## Testing

### Manual Testing
1. Start Kafka: `docker-compose up -d`
2. Run sample: `dotnet run`
3. Watch logs for published/consumed messages
4. View messages in Kafka UI

### Integration Testing
Use the Kafka test containers for integration tests:

```csharp
// Example using Testcontainers.Kafka
var kafka = new KafkaContainer()
    .WithImage("confluentinc/cp-kafka:7.5.0");

await kafka.StartAsync();
```

## Troubleshooting

### Topics Don't Exist Error
```
Confluent.Kafka.ConsumeException: Subscribed topic not available: order-placed
```

**Solution:** Create the topics before running the application (see Quick Start step 2):
```bash
docker exec -it justsaying-kafka kafka-topics --create \
  --topic order-placed \
  --bootstrap-server localhost:9092 \
  --partitions 3 \
  --replication-factor 1
```

Alternatively, enable auto-creation in docker-compose.yml (not recommended for production):
```yaml
environment:
  KAFKA_AUTO_CREATE_TOPICS_ENABLE: 'true'
```

### Kafka Connection Issues
- Ensure Kafka is running: `docker-compose ps`
- Check bootstrap servers: `localhost:9092`
- Verify network connectivity

### Messages Not Being Consumed
- Check consumer group ID is set
- Verify topic names match
- Ensure CloudEvents settings match on publisher/consumer
- Check handler is registered

### CloudEvents Format Issues
- Verify `WithCloudEvents(true)` on both sides
- Check source URI is valid
- Inspect message in Kafka UI

## Clean Up

Stop and remove all containers:
```bash
docker-compose down -v
```

## References

- [JustSaying Documentation](https://github.com/justeat/JustSaying)
- [Apache Kafka](https://kafka.apache.org/)
- [CloudEvents Specification](https://cloudevents.io/)
- [Confluent Kafka .NET Client](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)

## License

This sample is part of the JustSaying project and follows the same license.
