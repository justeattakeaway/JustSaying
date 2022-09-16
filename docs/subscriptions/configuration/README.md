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

### [ForTopic&lt;T&gt;](fortopic.md)

### [ForQueue&lt;T&gt;](forqueue.md)

test
