---
---

# WithQueue

### `WithQueue<T>()`

Publishes messages of type `T` directly to an SQS queue without using SNS. This is ideal for point-to-point messaging where only one consumer should process each message, typically used for command patterns.

* An SQS queue will be created for messages of type `T`.
* An error queue will be created with an `_error` suffix (unless explicitly disabled).
* The queue name will be determined using the supplied (or default if not) `IQueueNamingConvention`, applied to the message type `T`.
  * This convention can be overridden by passing a [destination](/destinations/) such as `QueueDestination.Named("...")`, or with `WithQueueName` in the queue configuration.

#### Example:

```csharp
config.Publications(x =>
{
    x.WithQueue<ProcessPaymentCommand>();
});
```

This describes the following infrastructure:

* An SQS queue of name `processpaymentcommand`
* An SQS error queue of name `processpaymentcommand_error`

### `WithQueue<T>(QueueDestination destination)`

Publishes messages of type `T` to the queue described by a [`QueueDestination`](/destinations/): an owned queue by name or convention, with settings for how it's created, or an existing queue by ARN or URL.

```csharp
config.Publications(x =>
{
    x.WithQueue<ProcessPaymentCommand>(QueueDestination.Named("payment-processing-queue", q => q
        .WithMessageRetention(TimeSpan.FromDays(7))
        .WithEncryption("alias/payments")));
});
```

The queue is usually read by a subscriber in another service, which owns the rest of its settings. So on startup a publication only sets the settings its destination declares, and leaves everything else on an existing queue alone. See [Infrastructure on Startup](/destinations/startup-behaviour).

### `WithQueueArn<T>(string queueArn)`, `WithQueueUrl<T>(string queueUrl)`, `WithQueueUri<T>(Uri queueUri)`

Publishes messages of type `T` to an existing SQS queue specified by its ARN, URL or URI. JustSaying never creates or changes the queue. These are the same as passing `QueueDestination.FromArn`, `QueueDestination.FromUrl` or `QueueDestination.FromUri`.

```csharp
config.Publications(x =>
{
    x.WithQueueArn<ProcessPaymentCommand>("arn:aws:sqs:us-east-1:123456789012:existing-queue");
    x.WithQueueUrl<RefundPaymentCommand>("https://sqs.us-east-1.amazonaws.com/123456789012/my-queue");
});
```

You can also pass a configuration lambda for existing queues. Use `WithQueueExistenceCheck()` to verify that the queue exists when the bus starts. Note this check requires the `sqs:GetQueueAttributes` permission.

```csharp
config.Publications(x =>
{
    x.WithQueueArn<ProcessPaymentCommand>(
        "arn:aws:sqs:us-east-1:123456789012:existing-queue",
        cfg => cfg.WithQueueExistenceCheck());
});
```

## Configuration Options

Every overload takes an optional `Action<QueuePublicationBuilder<T>>`:

#### `WithQueueName(string name)`

Override the naming convention to use a specific queue name. This is the same as using `QueueDestination.Named(name)`, so don't do both.

#### `WithQueueExistenceCheck()`

Verify that an existing SQS queue can be found before the bus starts. If the queue does not exist, startup fails with a clear exception instead of waiting until publish attempts fail. Only applies to an existing queue: a queue JustSaying owns is created on startup.

#### `WithSubject(string subject)`

Set the `Subject` written into the queue message, instead of the message type's name.

#### `WithRawMessages()`

Send the message body as it is, without JustSaying's `{ "Subject": ..., "Message": ... }` wrapper. Use this when the consumer isn't JustSaying. A JustSaying subscriber reading the queue should use `WithRawMessageDelivery()`, so that the body is never mistaken for the wrapper.

#### `WithCompression(PublishCompressionOptions options)`

Compress large message bodies. See [Compression](../advanced/compression.md).

#### `WithMiddlewareConfiguration(Action<PublishMiddlewareBuilder> middlewareConfiguration)`

Configure a per-publisher middleware pipeline for this queue. When set, this takes priority over the global publish middleware. See [Publish Middleware](middleware.md) for details.

```csharp
x.WithQueue<ProcessPaymentCommand>(cfg =>
{
    cfg.WithMiddlewareConfiguration(m =>
    {
        m.Use<LoggingPublishMiddleware>();
    });
});
```

## When to Use Queues

Use queues when:
- Only one consumer should process each message (point-to-point delivery)
- You're publishing commands that instruct a service to do something
- You need guaranteed message delivery to a single service
- You want to avoid the overhead of SNS topic management

For fan-out scenarios where multiple services need to react to the same message, use [WithTopic](withtopic.md) instead.

## Topics vs Queues

| Feature | Queues (WithQueue) | Topics (WithTopic) |
|---------|-------------------|-------------------|
| **Pattern** | Point-to-Point | Publish-Subscribe |
| **Consumers** | Single consumer | Multiple consumers |
| **AWS Service** | SQS only | SNS + SQS |
| **Use Case** | Commands | Events |

## Error Handling

By default, JustSaying creates an error queue for each publication queue. Messages that fail to process are moved to the error queue for later inspection or reprocessing. You can opt out on the destination:

```csharp
x.WithQueue<ProcessPaymentCommand>(QueueDestination.ByConvention(q => q.WithNoErrorQueue()));
```
