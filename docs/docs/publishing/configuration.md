---
---

# Configuration

All publication configuration can be accessed via the `MessagingBusBuilder.Publications` fluent API:

```csharp
services.AddJustSaying(config =>
{
    config.Publications(x =>
    {
        // Configure publications here
    });
});
```

The `Publications` builder provides methods to define where and how messages are published in your messaging topology. Each method takes a [destination](/destinations/), which says which topic or queue to use and how to create it, and an optional callback to configure how messages are published to it.

## Publication Types

### Topic Publications (SNS)

Topics enable a publish-subscribe pattern where multiple subscribers can receive the same message.

```csharp
config.Publications(x =>
{
    // Owned topic, named by convention
    x.WithTopic<OrderPlacedEvent>();

    // Owned topic with a name and tags
    x.WithTopic<OrderReadyEvent>(TopicDestination.Named("order-ready", t => t
        .WithTag("IsOrderEvent", "true")));

    // Existing topic by ARN
    x.WithTopic<OrderCancelledEvent>(TopicDestination.FromArn("arn:aws:sns:us-east-1:123456789012:my-topic"));
});
```

### Queue Publications (SQS)

Queues enable point-to-point messaging where only one consumer processes each message.

```csharp
config.Publications(x =>
{
    // Owned queue, named by convention
    x.WithQueue<ProcessPaymentCommand>();

    // Owned queue with a name and settings
    x.WithQueue<RefundPaymentCommand>(QueueDestination.Named("payment-refunds", q => q
        .WithMessageRetention(TimeSpan.FromDays(7))));

    // Existing queue by URL, checked when the bus starts
    x.WithQueue<CapturePaymentCommand>(
        QueueDestination.FromUrl("https://sqs.us-east-1.amazonaws.com/123456789012/my-queue"),
        cfg => cfg.WithQueueExistenceCheck());
});
```

## Choosing Between Topics and Queues

Use **topics** when:
- Multiple services need to react to the same event
- You need a fan-out pattern
- Publishing events that represent something that happened

Use **queues** when:
- Only one consumer should process the message
- You need point-to-point delivery
- Publishing commands for a specific service

## Available Methods

### Topic Methods

- `WithTopic<T>()` - Create/use a topic with name from naming convention
- `WithTopic<T>(Action<TopicPublicationBuilder<T>>)` - The same, with publication settings
- `WithTopic<T>(TopicDestination, Action<TopicPublicationBuilder<T>> = null)` - Publish to the topic described by the destination
- `WithTopicArn<T>(string, Action<TopicPublicationBuilder<T>> = null)` - Publish to existing topic by ARN

### Queue Methods

- `WithQueue<T>()` - Create/use a queue with name from naming convention
- `WithQueue<T>(Action<QueuePublicationBuilder<T>>)` - The same, with publication settings
- `WithQueue<T>(QueueDestination, Action<QueuePublicationBuilder<T>> = null)` - Publish to the queue described by the destination
- `WithQueueArn<T>(string)`, `WithQueueArn<T>(string, Action<QueuePublicationBuilder<T>>)` - Publish to existing queue by ARN
- `WithQueueUrl<T>(string)`, `WithQueueUrl<T>(string, Action<QueuePublicationBuilder<T>>)` - Publish to existing queue by URL
- `WithQueueUri<T>(Uri)`, `WithQueueUri<T>(Uri, Action<QueuePublicationBuilder<T>>)` - Publish to existing queue by URI

For existing queues, the queue publication builder supports `WithQueueExistenceCheck()`, which verifies the queue during bus startup.

### Rules

- Each message type can only have one publication. A second registration for the same type throws when the bus is built.
- Messages are routed by their runtime type: a message goes to the publication for its own type, or else for its closest base class or an interface it implements. See [Message Types](/messages/#publishing-to-a-base-type).

## Further Reading

- [WithTopic](withtopic.md) - Detailed topic publication configuration
- [WithQueue](withqueue.md) - Detailed queue publication configuration
- [Destinations](/destinations/) - Naming and creating topics and queues
- [Publication Settings](write-configuration.md) - Subject, compression, raw messages and error handling
- [Batch Publishing](batch-publishing.md) - Publishing multiple messages efficiently
