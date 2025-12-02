# Quick Start Guide - Kafka Transport with CloudEvents

This guide will get you up and running with the JustSaying Kafka extension in 5 minutes.

## Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose (for local Kafka)

## Step 1: Start Kafka Locally

```bash
cd samples/src/JustSaying.Sample.Kafka
docker-compose up -d
```

This starts:
- Kafka broker on `localhost:9092`
- Zookeeper on `localhost:2181`
- Kafka UI on `http://localhost:8080`

Verify Kafka is running:
```bash
docker-compose ps
```

## Step 2: Run the Sample Application

```bash
cd samples/src/JustSaying.Sample.Kafka
dotnet run
```

You should see output like:
```
[10:30:15 INF] Starting Kafka Sample Application with CloudEvents support
[10:30:16 INF] Subscribed to Kafka topic 'order-placed'
[10:30:16 INF] Subscribed to Kafka topic 'order-confirmed'
[10:30:18 INF] Published OrderPlacedEvent for ORD-00001
[10:30:18 INF] Processing order ORD-00001 for customer CUST-042. Amount: $125.00
[10:30:19 INF] Published OrderConfirmedEvent for ORD-00001
[10:30:19 INF] Order ORD-00001 confirmed by AutomatedSystem
```

## Step 3: View Messages in Kafka UI

Open http://localhost:8080 in your browser to see:
- Topics: `order-placed`, `order-confirmed`
- CloudEvents formatted messages
- Message metadata and headers

## Step 4: Inspect CloudEvents Messages

Click on a message in the Kafka UI to see the CloudEvents structure:

```json
{
  "specversion": "1.0",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "type": "JustSaying.Sample.Kafka.Messages.OrderPlacedEvent",
  "source": "urn:justsaying:sample:orders",
  "time": "2024-12-02T10:30:00Z",
  "datacontenttype": "application/json",
  "subject": "OrderPlacedEvent",
  "data": {
    "orderId": "ORD-00001",
    "customerId": "CUST-042",
    "amount": 125.00,
    "orderDate": "2024-12-02T10:30:00Z",
    "items": [
      {
        "productId": "PROD-001",
        "productName": "Widget",
        "quantity": 2,
        "unitPrice": 25.00
      }
    ]
  },
  "raisingcomponent": "OrderService",
  "tenant": "tenant-demo"
}
```

## Step 5: Create Your Own Message

1. Define your message:

```csharp
public class MyCustomEvent : Message
{
    public string MyProperty { get; set; }
}
```

2. Configure publisher:

```csharp
config.WithKafkaPublisher<MyCustomEvent>("my-topic", kafka =>
{
    kafka.BootstrapServers = "localhost:9092";
    kafka.EnableCloudEvents = true;
});
```

3. Publish:

```csharp
await publisher.PublishAsync(new MyCustomEvent 
{ 
    MyProperty = "Hello Kafka!" 
});
```

## What Just Happened?

1. ✅ **Publisher**: Converted your `Message` to CloudEvents format
2. ✅ **Kafka**: Stored the event in a topic with all metadata
3. ✅ **Consumer**: Received and converted CloudEvents back to your `Message`
4. ✅ **Handler**: Processed the message using the standard `IHandlerAsync<T>` interface

## Key Concepts Demonstrated

- **CloudEvents Compliance**: Messages follow the CloudEvents v1.0 specification
- **Message Compatibility**: Your existing `Message` classes work without changes
- **Bidirectional Conversion**: Seamless conversion between Message and CloudEvents
- **Metadata Preservation**: All JustSaying metadata (Id, TimeStamp, RaisingComponent, etc.) is preserved

## Next Steps

1. **Explore the Sample Code**: Check out `Program.cs` to see how it's configured
2. **Read the README**: See [README.md](README.md) for full API documentation
3. **Migration Guide**: Review [MIGRATION.md](MIGRATION.md) for migrating from SNS/SQS
4. **Customize**: Modify the sample to fit your use case

## Troubleshooting

**Kafka not starting?**
```bash
docker-compose down -v
docker-compose up -d
```

**Messages not appearing?**
- Check Kafka UI at http://localhost:8080
- Verify topic exists
- Check application logs for errors

**Consumer not receiving messages?**
- Ensure consumer group ID is set
- Check consumer is subscribed to correct topic
- Verify CloudEvents enabled on both publisher and consumer

## Clean Up

Stop and remove all containers:
```bash
docker-compose down -v
```

## Further Reading

- [CloudEvents Specification](https://cloudevents.io/)
- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)
- [JustSaying Documentation](https://justeat.gitbook.io/justsaying/)

---

**Congratulations!** You're now running JustSaying with Kafka and CloudEvents. 🎉
