---
---

# WithQueue

### `WithQueue<T>()`

Publishes messages of type `T` directly to an SQS queue without using SNS. This is ideal for point-to-point messaging where only one consumer should process each message, typically used for command patterns.

* An SQS queue will be created for messages of type `T`.
* An error queue will be created with an `_error` suffix \(unless explicitly disabled\).
* The queue name will be determined using the supplied \(or default if not\) `IQueueNamingConvention`, applied to the message type `T`.
  * This convention can be overridden using `WithQueueName` in the queue configuration.

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

Further configuration options can be defined by passing a configuration lambda to the `WithQueue` method.

#### Example with configuration:

```csharp
config.Publications(x =>
{
    x.WithQueue<ProcessPaymentCommand>(cfg =>
    {
        cfg.WithQueueName("payment-processing-queue");
    });
});
```

### `WithQueueArn<T>(string queueArn)`

Publishes messages of type `T` to an existing SQS queue specified by its ARN \(Amazon Resource Name\).

#### Example:

```csharp
config.Publications(x =>
{
    x.WithQueueArn<ProcessPaymentCommand>("arn:aws:sqs:us-east-1:123456789012:existing-queue");
});
```

### `WithQueueUrl<T>(string queueUrl)`

Publishes messages of type `T` to an existing SQS queue specified by its URL.

#### Example:

```csharp
config.Publications(x =>
{
    x.WithQueueUrl<ProcessPaymentCommand>("https://sqs.us-east-1.amazonaws.com/123456789012/my-queue");
});
```

### `WithQueueUri<T>(Uri queueUri)`

Publishes messages of type `T` to an existing SQS queue specified by its URI.

#### Example:

```csharp
config.Publications(x =>
{
    x.WithQueueUri<ProcessPaymentCommand>(new Uri("https://sqs.us-east-1.amazonaws.com/123456789012/my-queue"));
});
```

## Configuration Options

When using the configuration lambda with `WithQueue`, the following options are available:

#### `WithQueueName(string name)`

Override the naming convention to use a specific queue name.

```csharp
x.WithQueue<ProcessPaymentCommand>(cfg =>
{
    cfg.WithQueueName("custom-payment-queue");
});
```

#### `WithWriteConfiguration(Action<SqsWriteConfigurationBuilder> configure)`

Configure advanced publishing options such as encryption, message retention, and error queues. See [Write Configuration](write-configuration.md) for details.

```csharp
x.WithQueue<ProcessPaymentCommand>(cfg =>
{
    cfg.WithWriteConfiguration(w =>
    {
        w.WithEncryption("your-kms-key-id");
        w.WithMessageRetention(TimeSpan.FromDays(7));
    });
});
```

## When to Use Queues

Use queues when:
- Only one consumer should process each message \(point-to-point delivery\)
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

By default, JustSaying creates an error queue for each publication queue. Messages that fail to process are moved to the error queue for later inspection or reprocessing. You can disable this behavior in the write configuration:

```csharp
x.WithQueue<ProcessPaymentCommand>(cfg =>
{
    cfg.WithWriteConfiguration(w =>
    {
        w.WithNoErrorQueue();
    });
});
```
