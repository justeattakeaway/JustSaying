---
---

# ForQueue

### `ForQueue<T>`

#### Creates a direct subscription to a queue, without a topic. This can be useful for direct 'command' style scenarios where the destination queue is already known at publish time.

* A queue will be created for message of type `T`, using the supplied `IQueueNamingConvention`, applied to the message type `T`.
  * This convention can be overridden on a case-by-case basis with a [destination](/destinations/) or `WithQueueName`.
* A dead letter queue will be created, named after the queue name above with an `_error` suffix.

#### Example:

```csharp
x.ForQueue<OrderReadyEvent>();
```

This describes the following infrastructure:

* An SQS queue of name `orderreadyevent`
* An SQS queue of name `orderreadyevent_error`

Further configuration options can be defined by passing a configuration lambda to the `ForQueue` method.

### `ForQueue<T>(QueueDestination destination)`

Subscribes to the queue described by a [`QueueDestination`](/destinations/): an owned queue by name or convention, with settings for how it's created, or an existing queue by ARN or URL.

```csharp
x.ForQueue<OrderReadyEvent>(QueueDestination.Named("order-ready", q => q
    .WithVisibilityTimeout(TimeSpan.FromMinutes(5))
    .WithNoErrorQueue()));
```

:::warning
On every start, a subscription sets its queue to the settings its destination declares and resets the others to their defaults. See [Infrastructure on Startup](/destinations/startup-behaviour).
:::

### `ForQueueArn<T>(string queueArn)`, `ForQueueUrl<T>(string queueUrl)`, `ForQueueUri<T>(Uri queueUri)`

Subscribes to an existing SQS queue specified by its ARN, URL or URI. JustSaying will not create or change the queue. These are the same as passing `QueueDestination.FromArn`, `QueueDestination.FromUrl` or `QueueDestination.FromUri`.

```csharp
x.ForQueueArn<OrderReadyEvent>("arn:aws:sqs:us-east-1:123456789012:existing-queue");
x.ForQueueUrl<OrderPlacedEvent>("https://sqs.us-east-1.amazonaws.com/123456789012/my-queue");
```

## Configuration Options

The configuration lambda receives a `QueueSubscriptionBuilder<T>`:

| Method | Description |
| --- | --- |
| `WithQueueName(name)` | Name the queue instead of using the naming convention. |
| `WithQueueExistenceCheck()` | Check that an existing queue can be found when the bus starts, instead of repeatedly failing receive requests. Only for an existing queue. Requires the `sqs:GetQueueAttributes` permission. |
| `WithRawMessageDelivery()` | The queue's messages are the bodies themselves, without JustSaying's `{ "Subject", "Message" }` wrapper or an SNS notification. |
| `WithSubscriptionGroup(name)` | Put the queue in a [subscription group](../subscriptiongroups/). |
| `WithMessageBodySerializer(serializer)` | Read this subscription's messages with its own [serializer](/messages/serialization). |
| `WithMiddlewareConfiguration(...)` | Replace the [middleware](../middleware/) pipeline. Call it last: it returns `ISubscriptionBuilder<T>`, not the queue builder. |

```csharp
x.ForQueueUrl<OrderReadyEvent>(
    "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue",
    configure: cfg => cfg
        .WithQueueExistenceCheck()
        .WithRawMessageDelivery());
```

`ForQueueUrl` and `ForQueueUri` also take an optional region name, which must agree with the URL. When it's left out, the region is read from the URL.

A queue can only have one subscription: to receive several message types on one queue, use a [multi-type queue](multi-type-queues.md).
