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

The `Publications` builder provides methods to define where and how messages are published in your messaging topology.

## Publication Types

### Topic Publications (SNS)

Topics enable a publish-subscribe pattern where multiple subscribers can receive the same message.

```csharp
config.Publications(x =>
{
    // Managed topic (naming convention based)
    x.WithTopic<OrderPlacedEvent>();

    // Managed topic with configuration
    x.WithTopic<OrderReadyEvent>(cfg =>
    {
        cfg.WithTag("IsOrderEvent", "true");
    });

    // Existing topic by ARN
    x.WithTopicArn<OrderPlacedEvent>("arn:aws:sns:us-east-1:123456789012:my-topic");
});
```

### Queue Publications (SQS)

Queues enable point-to-point messaging where only one consumer processes each message.

```csharp
config.Publications(x =>
{
    // Managed queue (naming convention based)
    x.WithQueue<ProcessPaymentCommand>();

    // Managed queue with configuration
    x.WithQueue<ProcessPaymentCommand>(cfg =>
    {
        cfg.WithQueueName("payment-processing-queue");
    });

    // Existing queue by URL
    x.WithQueueUrl<ProcessPaymentCommand>("https://sqs.us-east-1.amazonaws.com/123456789012/my-queue");
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
- `WithTopic<T>(Action<TopicPublicationBuilder<T>>)` - Configure topic publication
- `WithTopicArn<T>(string)` - Publish to existing topic by ARN

### Queue Methods

- `WithQueue<T>()` - Create/use a queue with name from naming convention
- `WithQueue<T>(Action<QueuePublicationBuilder<T>>)` - Configure queue publication
- `WithQueueArn<T>(string)` - Publish to existing queue by ARN
- `WithQueueUrl<T>(string)` - Publish to existing queue by URL
- `WithQueueUri<T>(Uri)` - Publish to existing queue by URI

## Further Reading

- [WithTopic](withtopic.md) - Detailed topic publication configuration
- [WithQueue](withqueue.md) - Detailed queue publication configuration
- [Write Configuration](write-configuration.md) - Encryption, compression, and advanced options
- [Batch Publishing](batch-publishing.md) - Publishing multiple messages efficiently

