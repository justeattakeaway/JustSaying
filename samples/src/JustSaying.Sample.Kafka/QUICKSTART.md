# Quick Start Guide - Kafka Extension for JustSaying

This guide demonstrates how to use Kafka alongside AWS SQS/SNS with the unified JustSaying API.

## Prerequisites

- .NET 8.0 SDK or later
- Docker and Docker Compose (for local Kafka)

## Step 1: Start Kafka Locally

```bash
cd samples/src/JustSaying.Sample.Kafka
docker-compose up -d
```

This starts:
- Kafka broker on `localhost:9092`
- Zookeeper on `localhost:2181`
- Kafka UI on `http://localhost:8080` (optional)

Verify Kafka is running:
```bash
docker-compose ps
```

## Step 2: Run the Sample Application

```bash
dotnet run
```

You should see output like:
```
info: JustSaying.Sample.Kafka.Program[0]
      Starting Kafka Sample Application with JustSaying
info: JustSaying.Sample.Kafka.Program[0]
      This sample demonstrates:
info: JustSaying.Sample.Kafka.Program[0]
        - Publishing messages to Kafka topics
info: JustSaying.Sample.Kafka.Program[0]
        - Consuming messages from Kafka topics
info: JustSaying.Sample.Kafka.Program[0]
        - CloudEvents format support
info: JustSaying.Sample.Kafka.Program[0]
        - Unified API alongside AWS SQS/SNS
info: JustSaying.Sample.Kafka.Services.MessageGeneratorService[0]
      ✅ Published OrderPlacedEvent for ORD-00001
info: JustSaying.Sample.Kafka.Handlers.OrderPlacedEventHandler[0]
      Processing order ORD-00001 for customer CUST-042. Amount: $125.00
info: JustSaying.Sample.Kafka.Services.MessageGeneratorService[0]
      ✅ Published OrderConfirmedEvent for ORD-00001
info: JustSaying.Sample.Kafka.Handlers.OrderConfirmedEventHandler[0]
      Order ORD-00001 confirmed by AutomatedSystem at 12/02/2025 10:30:19
```

## Step 3: Understanding the Code

### Global Kafka Configuration

The sample uses global Kafka configuration to avoid repetition:

```csharp
builder.Messaging(config =>
{
    config.WithRegion("us-east-1"); // Required for AWS compatibility
    
    // Set global Kafka defaults - applies to all topics
    config.WithKafka(kafka =>
    {
        kafka.BootstrapServers = "localhost:9092";
        kafka.EnableCloudEvents = true;
        kafka.CloudEventsSource = "urn:justsaying:sample:orders";
    });
});
```

### Configure Publications (Publishing to Kafka)

With global configuration, you only need to specify the topic:

```csharp
builder.Publications(pubs =>
{
    // Inherits BootstrapServers and CloudEvents settings from global config
    pubs.WithKafka<OrderPlacedEvent>("order-placed");
});
```

### Configure Subscriptions (Consuming from Kafka)

Subscriptions inherit global settings, you just add the consumer group:

```csharp
builder.Subscriptions(subs =>
{
    subs.ForKafka<OrderPlacedEvent>("order-placed", kafka =>
    {
        kafka.WithGroupId("sample-consumer-group");
        // BootstrapServers and CloudEvents inherited from global config
    });
});
```

### Per-Topic Override (Optional)

You can still override settings for specific topics:

```csharp
builder.Publications(pubs =>
{
    pubs.WithKafka<OrderPlacedEvent>("order-placed", kafka =>
    {
        kafka.WithBootstrapServers("different-kafka:9092"); // Override global
    });
});
```

### Register Handlers

```csharp
services.AddJustSayingHandler<OrderPlacedEvent, OrderPlacedEventHandler>();
```

## Step 4: Create Your Own Messages

1. **Define your message** (inherits from `JustSaying.Models.Message`):

```csharp
public class MyCustomEvent : Message
{
    public string MyProperty { get; set; }
    public int MyValue { get; set; }
}
```

2. **Create a handler**:

```csharp
public class MyCustomEventHandler : IHandlerAsync<MyCustomEvent>
{
    private readonly ILogger<MyCustomEventHandler> _logger;

    public MyCustomEventHandler(ILogger<MyCustomEventHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> Handle(MyCustomEvent message)
    {
        _logger.LogInformation(
            "Received: {Property} = {Value}", 
            message.MyProperty, 
            message.MyValue);
        return Task.FromResult(true);
    }
}
```

3. **Configure in Program.cs**:

```csharp
// Register handler
services.AddJustSayingHandler<MyCustomEvent, MyCustomEventHandler>();

// Configure publication
builder.Publications(pubs =>
{
    pubs.WithKafka<MyCustomEvent>("my-topic", kafka =>
    {
        kafka.WithBootstrapServers("localhost:9092")
             .WithCloudEvents(true);
    });
});

// Configure subscription
builder.Subscriptions(subs =>
{
    subs.ForKafka<MyCustomEvent>("my-topic", kafka =>
    {
        kafka.WithBootstrapServers("localhost:9092")
             .WithGroupId("my-consumer-group")
             .WithCloudEvents(true);
    });
});
```

4. **Publish messages**:

```csharp
public class MyService
{
    private readonly IMessagePublisher _publisher;

    public MyService(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task DoSomething()
    {
        await _publisher.PublishAsync(new MyCustomEvent 
        { 
            MyProperty = "Hello Kafka!",
            MyValue = 42
        });
    }
}
```

## What's Happening?

1. **Unified API**: Same `AddJustSaying()` API as AWS SQS/SNS
2. **CloudEvents**: Messages are automatically wrapped in CloudEvents format
3. **Message Preservation**: All JustSaying metadata (Id, TimeStamp, etc.) is preserved
4. **Middleware Support**: Full middleware pipeline support just like SQS
5. **Multi-Transport**: Use Kafka and AWS transports side-by-side

## Key Features Demonstrated

- ✅ **Declarative Subscriptions**: Use `ForKafka<T>()` pattern like `ForQueue<T>()`
- ✅ **Declarative Publications**: Use `WithKafka<T>()` pattern  
- ✅ **CloudEvents Support**: Automatic CloudEvents v1.0 formatting
- ✅ **Middleware Integration**: Same middleware pipeline as SQS/SNS
- ✅ **Message Handlers**: Standard `IHandlerAsync<T>` interface
- ✅ **Multi-Transport**: Mix Kafka and AWS in the same application

## Viewing Messages in Kafka

If you have Kafka UI running at http://localhost:8080, you can:
- Browse topics: `order-placed`, `order-confirmed`
- Inspect CloudEvents formatted messages
- View consumer groups and their offsets

## Mixing Transports

You can use both Kafka and AWS SQS/SNS in the same application:

```csharp
builder.Publications(pubs =>
{
    // Kafka publication
    pubs.WithKafka<OrderPlacedEvent>("order-placed", kafka => 
    {
        kafka.WithBootstrapServers("localhost:9092");
    });
    
    // SNS publication
    pubs.WithTopic<OrderPlacedEvent>(sns => 
    {
        sns.WithWriteConfiguration(c => c.QueueName = "order-placed");
    });
});

builder.Subscriptions(subs =>
{
    // Kafka subscription
    subs.ForKafka<OrderPlacedEvent>("order-placed", kafka => 
    {
        kafka.WithBootstrapServers("localhost:9092")
             .WithGroupId("my-group");
    });
    
    // SQS subscription  
    subs.ForQueue<OrderConfirmedEvent>(sqs =>
    {
        sqs.WithReadConfiguration(c => c.QueueName = "order-confirmed");
    });
});
```

## Troubleshooting

**Kafka not starting?**
```bash
docker-compose down -v
docker-compose up -d
```

**Messages not appearing?**
- Check logs for exceptions
- Verify Kafka is running: `docker-compose ps`
- Check topic exists in Kafka UI

**Consumer not receiving?**
- Ensure `WithGroupId()` is set
- Verify topic names match between publisher and subscriber
- Check CloudEvents settings match on both sides

## Clean Up

Stop and remove all containers:
```bash
docker-compose down -v
```

## Next Steps

1. **Explore the code**: Check out `Program.cs`, handlers, and messages
2. **Add more messages**: Create your own message types
3. **Customize configuration**: Add producer/consumer configurations
4. **Mix transports**: Try using both Kafka and AWS SQS/SNS

## Further Reading

- [CloudEvents Specification](https://cloudevents.io/)
- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)
- [JustSaying Documentation](https://github.com/justeat/JustSaying)

---

**Congratulations!** You're now using Kafka with JustSaying's unified API. 🎉
