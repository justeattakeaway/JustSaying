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

## Available Methods

### Topic Publications

- [WithTopic\<T\>()](withtopic.md) - Publish to an SNS topic \(creates if not exists\)
- WithTopicArn\<T\>(arn) - Publish to an existing topic by ARN

### Queue Publications

- [WithQueue\<T\>()](withqueue.md) - Publish directly to an SQS queue \(creates if not exists\)
- WithQueueArn\<T\>(arn) - Publish to an existing queue by ARN
- WithQueueUrl\<T\>(url) - Publish to an existing queue by URL
- WithQueueUri\<T\>(uri) - Publish to an existing queue by URI

## Further Configuration

For detailed configuration options, see:

- [Configuration](configuration.md) - Overview of publication configuration
- [Write Configuration](write-configuration.md) - Encryption, compression, and advanced options
- [Batch Publishing](batch-publishing.md) - Publishing multiple messages efficiently
