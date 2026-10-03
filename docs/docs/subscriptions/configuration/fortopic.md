---
---

# ForTopic

### `ForTopic<T>`

#### Creates a topic for which there can be multiple queue subscriptions. This enables a 'fan out' scenario.

* A topic and queue pair will be created for message of type `T`, with a subscription attaching the topic to the queue.
* The topic and queue names will be determined using the supplied \(or default if not\) `ITopicNamingConvention` and `IQueueNamingConvention`, applied to the message type `T`.
  * These conventions can be overridden on a case-by-case basis with a [destination](/destinations/), or `WithTopicName` and `WithQueueName`.
* A dead letter queue will be created, named after the queue name above with an `_error` suffix.

#### Example:

```csharp
x.ForTopic<OrderReadyEvent>();
```

This describes the following infrastructure:

* An SQS queue of name `orderreadyevent`
* An SQS queue of name `orderreadyevent_error`
* An SNS topic of name `orderreadyevent`
* An SNS topic subscription on topic `orderreadyevent` and queue `orderreadyevent`

Further configuration options can be defined by passing a configuration lambda to the `ForTopic` method.

### Naming the topic and the queue

Pass a `TopicDestination` to name the topic, and give the subscription's queue a `QueueDestination` with `WithQueue`, including any settings for creating it:

```csharp
x.ForTopic<OrderReadyEvent>(TopicDestination.Named("order-ready"), c => c
    .WithQueue(QueueDestination.Named("kitchen-order-ready", q => q
        .WithVisibilityTimeout(TimeSpan.FromMinutes(2))
        .WithRetriesBeforeErrorQueue(3)
        .WithTag("team", "kitchen"))));
```

`WithTopicName(name)` and `WithQueueName(name)` do the same as `Named(name)` without any settings. Several services can subscribe to one topic by giving each subscription its own queue.

A topic subscription creates its topic if it's missing, but it doesn't take topic settings: configure tags and encryption on the [publication](/publishing/withtopic). It also can't target a topic by ARN, or a queue by ARN or URL. To subscribe to a topic in another account, name it and use `WithTopicSourceAccount`.

:::warning
On every start, a subscription sets its queue to the settings its destination declares and resets the others to their defaults. See [Infrastructure on Startup](/destinations/startup-behaviour).
:::

## Configuration Options

The configuration lambda receives a `TopicSubscriptionBuilder<T>`:

| Method | Description |
| --- | --- |
| `WithQueue(QueueDestination)` | The queue to subscribe, and how to create it. |
| `WithQueueName(name)`, `WithTopicName(name)` | Name the queue or topic instead of using the naming conventions. |
| `WithFilterPolicy(json)` | An [SNS filter policy](https://docs.aws.amazon.com/sns/latest/dg/sns-message-filtering.html), so only matching messages are delivered to the queue. |
| `WithRawMessageDelivery()` | Turn on SNS raw message delivery, so the queue receives the message body without the SNS notification around it. |
| `WithTopicSourceAccount(accountId)` | The AWS account that owns the topic, for a cross-account subscription. |
| `WithSubscriptionGroup(name)` | Put the queue in a [subscription group](../subscriptiongroups/). |
| `WithMessageBodySerializer(serializer)` | Read this subscription's messages with its own [serializer](/messages/serialization). |
| `WithMiddlewareConfiguration(...)` | Replace the [middleware](../middleware/) pipeline. Call it last: it returns `ISubscriptionBuilder<T>`, not the topic builder. |

```csharp
x.ForTopic<OrderPlacedEvent>(c => c
    .WithQueueName("marketing-orders")
    .WithFilterPolicy("""{ "Tenant": ["uk"] }""")
    .WithSubscriptionGroup("orders"));
```

For more settings and how they map from v8's `WithReadConfiguration`, see [Subscription Settings](sqsreadconfiguration.md).

A queue can only have one subscription. Two `ForTopic` registrations for *different* message types on the same queue throw when the bus is built; to receive several types on one queue, use a [multi-type queue](multi-type-queues.md). Subscribing one queue to several topics with the *same* message type is allowed.
