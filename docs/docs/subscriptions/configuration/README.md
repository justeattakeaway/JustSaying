---
description: >-
  JustSaying has many configuration options for its subscription back end. In
  these pages we'll discuss them and detail how to decide what they should be.
---

# Configuration

All subscription configuration can be accessed via the `MessagingBusBuilder.Subscriptions` fluent api:

```csharp
services.AddJustSaying((MessagingBusBuilder config) =>
{
    config.Subscriptions((SubscriptionsBuilder subscriptionConfig) =>
    {
        // here
    });

});
```

The `subscriptionConfig` builder provides methods to describe the topology of your messaging setup. Each method can take a [destination](/destinations/) to name the topic or queue and configure how it's created.

### [ForTopic&lt;T&gt;](/subscriptions/configuration/fortopic)

### [ForQueue&lt;T&gt;](/subscriptions/configuration/forqueue)

Queue subscriptions can also target existing queues by ARN, URL, or URI:

```csharp
subscriptionConfig.ForQueueArn<OrderReadyEvent>(
    "arn:aws:sqs:us-east-1:123456789012:existing-queue",
    cfg => cfg.WithQueueExistenceCheck());
```

Use `WithQueueExistenceCheck()` when you want JustSaying to verify an existing queue during bus startup. Note this check requires the `sqs:GetQueueAttributes` permission.

### [ForQueue (multi-type)](/subscriptions/configuration/multi-type-queues)

One queue that carries several message types, each dispatched to its own handler.

### [CloudEvents](/cloudevents/consuming)

`ForCloudEventTopic<T>`, `ForCloudEventTopicData<T>`, `HandlingCloudEvent<T>` and `HandlingCloudEventData<T>` consume CloudEvents, with the `JustSaying.CloudEvents` package.
