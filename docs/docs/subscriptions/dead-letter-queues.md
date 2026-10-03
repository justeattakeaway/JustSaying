---
description: AKA Error Queues
---

# Dead Letter Queues

JustSaying supports error queues and this option is enabled by default. When a handler is unable to handle a message, JustSaying will attempt to re-deliver the message up to 5 times and if the handler is still unable to handle the message then the message will be moved to an error queue, named after the queue with an `_error` suffix.

Both are configured on the queue's [destination](/destinations/#queue-settings):

```csharp
x.ForTopic<OrderReadyEvent>(c => c
    .WithQueue(QueueDestination.ByConvention(q => q
        .WithRetriesBeforeErrorQueue(3)
        .WithErrorQueueRetention(TimeSpan.FromDays(7)))));

x.ForQueue<OrderPlacedEvent>(QueueDestination.ByConvention(q => q.WithNoErrorQueue()));
```

Messages that can't be read also end up in the error queue: for example a message that fails to deserialize, a CloudEvent that isn't valid, or a message on a [multi-type queue](/subscriptions/configuration/multi-type-queues#unroutable-messages) that no registered type matches. They're left on the queue rather than deleted, so the redrive policy moves them to the error queue once their retries are used up. From there you can redrive them back to the queue when a consumer can handle them.

`WithNoErrorQueue()` stops JustSaying creating an error queue, but doesn't remove one that already exists. See [Infrastructure on Startup](/destinations/startup-behaviour).

