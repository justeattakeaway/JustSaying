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

## Best Practices

1. **Use CloudEvents**: Enable CloudEvents for better interoperability and event metadata
2. **Consumer Groups**: Use meaningful consumer group IDs for horizontal scaling
3. **Error Handling**: Implement proper error handling in message handlers
4. **Idempotency**: Design handlers to be idempotent using `Message.UniqueKey()`
5. **Monitoring**: Set up logging and monitoring for message processing
6. **Testing**: Use the test helpers to verify message handling logic

## Example: Complete Application

```csharp
using JustSaying;
using JustSaying.Extensions.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Add JustSaying with Kafka
                services.AddJustSaying(config =>
                {
                    config.Messaging(x => x.WithRegion("us-east-1"));
                    
                    config.WithKafkaPublisher<OrderPlacedEvent>("orders", kafka =>
                    {
                        kafka.BootstrapServers = "localhost:9092";
                        kafka.EnableCloudEvents = true;
                        kafka.CloudEventsSource = "urn:myapp:orders";
                    });
                });
                
                // Add background service for consuming
                services.AddHostedService<OrderProcessorService>();
            })
            .Build();

        await host.RunAsync();
    }
}

public class OrderProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private KafkaMessageConsumer _consumer;

    public OrderProcessorService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer = _serviceProvider.CreateKafkaConsumer("orders", kafka =>
        {
            kafka.BootstrapServers = "localhost:9092";
            kafka.GroupId = "order-processor";
        });

        var handler = _serviceProvider.GetRequiredService<OrderPlacedEventHandler>();
        await _consumer.StartAsync(handler, stoppingToken);
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
```

## Contributing

Contributions are welcome! Please see the [main JustSaying repository](https://github.com/justeattakeaway/JustSaying) for contribution guidelines.

## License

This project is licensed under the Apache 2.0 License - see the LICENSE file in the main JustSaying repository for details.
