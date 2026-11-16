---
title: Subscription Settings
---

# Subscription Settings

There are many toggles and settings available to tune JustSaying to your workload. Before v9 they were all set through `WithReadConfiguration`. In v9 they're split between:

* the subscription's queue [destination](/destinations/), for settings that belong to the queue itself;
* the subscription builder, for how the queue is subscribed to and read.

```csharp
x.ForTopic<OrderReadyEvent>(c => c
    .WithQueue(QueueDestination.Named("order-ready", q => q
        .WithVisibilityTimeout(TimeSpan.FromMinutes(5))
        .WithMessageRetention(TimeSpan.FromDays(7))
        .WithRetriesBeforeErrorQueue(3)
        .WithEncryption("alias/orders")))
    .WithSubscriptionGroup("orders")
    .WithMiddlewareConfiguration(m => m.UseDefaults<OrderReadyEvent>(typeof(OrderReadyEventHandler))));

x.ForQueue<OrderReadyEvent>(QueueDestination.Named("order-ready-commands", q => q
    .WithNoErrorQueue()));
```

Note that these configuration options are per-\(queue/topic\).

## Queue settings

These go on the `QueueDestination` passed to `ForQueue<T>(destination)`, or to `WithQueue(destination)` inside `ForTopic<T>`. See [Destinations](/destinations/#queue-settings) for the defaults and limits.

#### `WithEncryption`

Encrypts the queue, and its error queue, with a key from AWS Key Management Service.

#### `WithNoErrorQueue`

Specifies that no error queue should be created for this subscription. An error queue is created by default.

#### `WithRetriesBeforeErrorQueue`

Specifies how many times a message is received before it's moved to the error queue. The default is 5.

#### `WithErrorQueueRetention`

Specifies how long messages are kept on the error queue. The default is 14 days.

#### `WithMessageRetention`

Specifies the duration for which messages should be kept in this queue before being automatically deleted. The default is 4 days.

#### `WithVisibilityTimeout`

Specifies for how long a message should be invisible to other consumers while it is being handled. The default is 30 seconds. For messages that take a long time to handle, this should be increased to avoid duplicate handling.

#### `WithDeliveryDelay`

Specifies how long a new message is hidden before it can be received. The default is 0.

#### `WithTag`

Adds a tag to the queue and its error queue.

## Subscription settings

These go on the subscription builder.

#### `WithTopicSourceAccount`

Specifies that the topic for this subscription belongs to a different AWS account than the current one. When this is set, a cross-account topic subscription will be created so that messages delivered to the specified account's topic will be delivered to a queue in this account. `ForTopic` only.

#### `WithFilterPolicy`

An SNS subscription filter policy, so only matching messages are delivered to the queue. `ForTopic` only.

#### `WithRawMessageDelivery`

For `ForTopic`, turns on SNS raw message delivery. For `ForQueue`, declares that the queue's messages arrive without any envelope.

#### `WithSubscriptionGroup`

Specifies that this subscription belongs to a [subscription group](../subscriptiongroups/). By default, each queue or topic subscription gets its own group.

#### `WithMessageBodySerializer`

Reads this subscription's messages with its own [serializer](/messages/serialization).

#### `WithMiddlewareConfiguration`

Provides a way to customise the middleware pipeline for this subscription. For more information, see the documentation on [middleware](../middleware/). It returns `ISubscriptionBuilder<T>`, so call it last.

## Porting `WithReadConfiguration`

:::warning
Port every setting from a v8 `WithReadConfiguration` call; don't just delete it. A subscription resets the settings its destination doesn't declare to their defaults on every start, so a deleted setting changes the live queue on the next deploy. See [Infrastructure on Startup](/destinations/startup-behaviour).
:::

| v8 (`WithReadConfiguration(r => ...)`) | v9 |
| --- | --- |
| `r.WithVisibilityTimeout(...)`, `r.WithMessageRetention(...)`, `r.WithEncryption(...)`, `r.WithNoErrorQueue()` | The same methods on the `QueueDestination` |
| `r.RetryCountBeforeSendingToErrorQueue` | `WithRetriesBeforeErrorQueue(...)` on the `QueueDestination` |
| `r.ErrorQueueRetentionPeriod` | `WithErrorQueueRetention(...)` on the `QueueDestination` |
| `r.DeliveryDelay` | `WithDeliveryDelay(...)` on the `QueueDestination` |
| `c.WithTag(...)`, `r.Tags` | `WithTag(...)` on the `QueueDestination`. On `ForTopic`, the tag always went on the queue, never the topic. |
| `r.WithErrorQueue()` | Nothing: an error queue is the default |
| `r.WithSubscriptionGroup(...)`, `r.WithTopicSourceAccount(...)` | `c.WithSubscriptionGroup(...)`, `c.WithTopicSourceAccount(...)` |
| `r.FilterPolicy`, `r.RawMessageDelivery = true` | `c.WithFilterPolicy(...)`, `c.WithRawMessageDelivery()` |
| `r.TopicName`, `r.QueueName` | `c.WithTopicName(...)`, `c.WithQueueName(...)`, or a named destination |

The full mapping is in the [migration guide](https://github.com/justeattakeaway/JustSaying/blob/main/MIGRATION-v9.md#porting-v8-configuration-setting-by-setting).
