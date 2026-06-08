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

The `subscriptionConfig` builder provides methods to describe the topology of your messaging setup. 

### [ForTopic&lt;T&gt;](/subscriptions/configuration/fortopic)

### [ForQueue&lt;T&gt;](/subscriptions/configuration/forqueue)

Queue subscriptions can also target existing queues by ARN, URL, or URI:

```csharp
subscriptionConfig.ForQueueArn<OrderReadyEvent>(
    "arn:aws:sqs:us-east-1:123456789012:existing-queue",
    cfg => cfg.WithQueueExistenceCheck());
```

Use `WithQueueExistenceCheck()` when you want JustSaying to verify an existing queue during bus startup.
