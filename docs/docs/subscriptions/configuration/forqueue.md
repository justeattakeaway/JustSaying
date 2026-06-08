---
---

# ForQueue

### `ForQueue<T>`

#### Creates a direct subscription to a queue, without a topic. This can be useful for direct 'command' style scenarios where the destination queue is already known at publish time.

* A queue will be created for message of type `T`, using the supplied `IQueueNamingConvention`, applied to the message type `T`. 
  * This convention can be overridden on a case-by-case basis using `WithName` in the queue configuration.
* A dead letter queue will be created, named after the queue name above with an `_error` suffix.

#### Example:

```text
x.ForQueue<OrderReadyEvent>();
```

This describes the following infrastructure:

* An SQS queue of name `orderreadyevent`
* An SQS queue of name `orderreadyevent_error`

Further configuration options can be defined by passing a configuration lambda to the `ForQueue` method.

### `ForQueueArn<T>(string queueArn)`

Subscribes to an existing SQS queue specified by its ARN (Amazon Resource Name). JustSaying will not create the queue.

#### Example:

```csharp
x.ForQueueArn<OrderReadyEvent>("arn:aws:sqs:us-east-1:123456789012:existing-queue");
```

### `ForQueueUrl<T>(string queueUrl)`

Subscribes to an existing SQS queue specified by its URL. JustSaying will not create the queue.

#### Example:

```csharp
x.ForQueueUrl<OrderReadyEvent>("https://sqs.us-east-1.amazonaws.com/123456789012/my-queue");
```

### `ForQueueUri<T>(Uri queueUri)`

Subscribes to an existing SQS queue specified by its URI. JustSaying will not create the queue.

#### Example:

```csharp
x.ForQueueUri<OrderReadyEvent>(new Uri("https://sqs.us-east-1.amazonaws.com/123456789012/my-queue"));
```

## Existing Queue Configuration

When subscribing to an existing queue with `ForQueueArn`, `ForQueueUrl`, or `ForQueueUri`, you can pass a configuration lambda.

#### `WithQueueExistenceCheck()`

Verify that the existing SQS queue can be found before the bus starts receiving messages. If the queue does not exist, startup fails with a clear exception instead of repeatedly failing receive requests.

```csharp
x.ForQueueArn<OrderReadyEvent>(
    "arn:aws:sqs:us-east-1:123456789012:existing-queue",
    cfg => cfg.WithQueueExistenceCheck());
```

#### `WithReadConfiguration(Action<QueueAddressConfiguration> configure)`

Configure read options for the existing queue subscription.

```csharp
x.ForQueueUrl<OrderReadyEvent>(
    "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue",
    cfg =>
    {
        cfg.WithQueueExistenceCheck();
        cfg.WithReadConfiguration(read => read.RawMessageDelivery = true);
    });
```
