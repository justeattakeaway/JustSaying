---
---

# Publications

Publishing in JustSaying allows you to send messages to AWS SNS topics or SQS queues. Configure your publications using the fluent API within `AddJustSaying` to define where and how messages are published.

## Topics vs Queues

JustSaying supports publishing to both SNS topics and SQS queues, each suited for different messaging patterns:

| Feature | Topics (SNS) | Queues (SQS) |
|---------|--------------|--------------|
| **Pattern** | Publish-Subscribe (fan-out) | Point-to-Point |
| **Subscribers** | Multiple subscribers can receive the same message | Single consumer processes each message |
| **Use Case** | Events that multiple services need to react to | Commands for a specific service |
| **Example** | `OrderPlacedEvent` → notify inventory, shipping, and analytics | `ProcessPaymentCommand` → payment service only |

## Configuring Publications

All publication configuration is accessed via the `MessagingBusBuilder.Publications` fluent API:

```csharp
services.AddJustSaying(config =>
{
    config.Publications(x =>
    {
        // Publish to SNS topics
        x.WithTopic<OrderPlacedEvent>();

        // Publish to SQS queues
        x.WithQueue<ProcessPaymentCommand>();
    });
});
```

Messages can be any class or record; see [Message Types](/messages/). Each message type can have one publication, and messages are routed by their runtime type.

## Available Methods

### Topic Publications

- [`WithTopic<T>()`](withtopic.md) - Publish to an SNS topic named by convention (created if it doesn't exist)
- [`WithTopic<T>(TopicDestination)`](withtopic.md) - Publish to the topic described by a [destination](/destinations/): named, by convention, or an existing topic by ARN
- `WithTopicArn<T>(arn)` - Publish to an existing topic by ARN, the same as `WithTopic<T>(TopicDestination.FromArn(arn))`

### Queue Publications

- [`WithQueue<T>()`](withqueue.md) - Publish directly to an SQS queue named by convention (created if it doesn't exist)
- [`WithQueue<T>(QueueDestination)`](withqueue.md) - Publish to the queue described by a [destination](/destinations/): named, by convention, or an existing queue by ARN or URL
- `WithQueueArn<T>(arn)`, `WithQueueUrl<T>(url)`, `WithQueueUri<T>(uri)` - Publish to an existing queue

Existing queue publishers can be configured with `WithQueueExistenceCheck()` to verify the existence of the queue during bus startup. Note this check requires the `sqs:GetQueueAttributes` permission.

```csharp
x.WithQueue<ProcessPaymentCommand>(
    QueueDestination.FromArn("arn:aws:sqs:us-east-1:123456789012:payments"),
    cfg => cfg.WithQueueExistenceCheck());
```

### CloudEvents Publications

- [`WithCloudEventTopic<T>`, `WithCloudEventQueue<T>`](/cloudevents/publishing) - Publish as CloudEvents, with the `JustSaying.CloudEvents` package

## Further Configuration

For detailed configuration options, see:

- [Configuration](configuration.md) - Overview of publication configuration
- [Destinations](/destinations/) - Naming, creating and configuring topics and queues
- [Publication Settings](write-configuration.md) - Subject, compression, raw messages and error handling
- [Batch Publishing](batch-publishing.md) - Publishing multiple messages efficiently
- [Publish Middleware](middleware.md) - Add cross-cutting concerns to publish operations
