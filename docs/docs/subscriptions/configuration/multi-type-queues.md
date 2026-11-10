---
---

# Multi-Type Queues

A queue can carry more than one message type. Subscribe to it once with `ForQueue(name, ...)` and register every type it carries with `Handling<T>()`:

```csharp
x.ForQueue("orders", q => q
    .Handling<OrderPlaced>()
    .Handling<OrderCancelled>());

services.AddJustSayingHandler<OrderPlaced, OrderPlacedHandler>();
services.AddJustSayingHandler<OrderCancelled, OrderCancelledHandler>();
```

Each message is read as its own type and passed to that type's handler. The queue can be named, as above, or given as a [destination](/destinations/), owned or existing. It can't be named by convention, because the convention names a queue after a single message type:

```csharp
x.ForQueue(QueueDestination.Named("orders", q => q.WithVisibilityTimeout(TimeSpan.FromMinutes(1))), q => q
    .Handling<OrderPlaced>()
    .Handling<OrderCancelled>());

x.ForQueue(QueueDestination.FromUrl("https://sqs.eu-west-1.amazonaws.com/111122223333/billing"), q => q
    .Handling<InvoiceRaised>()
    .Handling<InvoicePaid>());
```

An existing queue can use `WithQueueExistenceCheck()` to check that it exists when the bus starts.

## Subscribing to topics

Messages can reach the queue from publications that send to it directly, with `WithQueue<T>(QueueDestination.Named("orders"))`, or from SNS topics. Subscribe the queue to each topic it should receive from:

```csharp
x.ForQueue("orders", q => q
    .Handling<OrderPlaced>()
    .Handling<OrderCancelled>()
    .SubscribeToTopic<OrderPlaced>()
    .SubscribeToTopic(TopicDestination.Named("cancellations"), """{ "region": ["eu"] }"""));
```

* `SubscribeToTopic<T>()` subscribes to the topic the naming convention names after `T`: the one `ForTopic<T>()` subscribes to, and a `WithTopic<T>()` publication publishes to.
* `SubscribeToTopic(TopicDestination.Named(name))` subscribes to a topic by name.
* Both take an optional SNS filter policy as a second argument, and use raw delivery if `WithRawMessageDelivery()` is called.

As with `ForTopic<T>`, on startup each topic is created if it doesn't exist, the queue is subscribed to it, and the queue's access policy allows the topic to send to it. A queue addressed by URL or ARN can't be subscribed to topics, because JustSaying never changes a queue it doesn't own. Topics can't be addressed by ARN or given infrastructure settings here: configure those on the publication.

## One subscription per queue

A queue can only have one JustSaying subscription. Two subscriptions on one queue would compete for its messages and read each other's as the wrong type, so the bus throws when it's built, and the error spells out the multi-type subscription to use instead. A queue is recognised however it's given: by name, or by the URL or ARN of a queue with that name in the same region.

In v8, two `ForTopic` registrations could share a queue. Port them to one multi-type subscription that handles both types and subscribes to both topics:

```csharp
// v8
x.ForTopic<OrderPlaced>(t => t.WithQueueName("orders"));
x.ForTopic<OrderCancelled>(t => t.WithQueueName("orders").WithFilterPolicy(policy));

// v9
x.ForQueue("orders", q => q
    .Handling<OrderPlaced>()
    .Handling<OrderCancelled>()
    .SubscribeToTopic<OrderPlaced>()
    .SubscribeToTopic<OrderCancelled>(policy));
```

Subscribing one queue to several topics with the *same* message type, through several `ForTopic<T>` registrations, is still allowed. Registering the same subscription twice, for example `ForTopic<T>()` twice, throws, because each message could be handled twice.

## How a message's type is found

Each type is identified on the wire by a **discriminator**. By default this is the `Subject` that JustSaying publishers set to the message type's name, so `Handling<OrderPlaced>()` matches messages with the subject `OrderPlaced`. Pass a name to match a different value, for example a subject set with `WithSubject` on the publication:

```csharp
x.ForQueue("orders", q => q
    .Handling<OrderPlaced>("order-placed")
    .Handling<OrderCancelled>("order-cancelled"));
```

Each type on a queue must resolve to a different name, or the bus throws when it's built.

### Custom discriminators

Add your own discriminator to read the type from somewhere else, such as a message attribute or the body. It implements `IMessageTypeDiscriminator`:

```csharp
public sealed class EventTypeAttributeDiscriminator : IMessageTypeDiscriminator
{
    public bool TryGetMessageTypeName(MessageDiscriminationContext context, out string typeName)
    {
        typeName = context.MessageAttributes.Get("event-type")?.StringValue;
        return typeName is not null;
    }
}

x.ForQueue("orders", q => q
    .WithDiscriminator(new EventTypeAttributeDiscriminator())
    .Handling<OrderPlaced>("order-placed")
    .Handling<OrderCancelled>("order-cancelled"));
```

`MessageDiscriminationContext` has the message's `Body` (unwrapped from any SNS or JustSaying envelope), its `Subject` and its `MessageAttributes`.

### The order discriminators run in

The discriminators run in a fixed order, whatever order you register types and discriminators in:

1. Discriminators added with `WithDiscriminator`, in the order added.
2. Discriminators added by a package, such as the [CloudEvents](/cloudevents/consuming#multi-type-queues) `type` discriminator.
3. The SNS `Subject`, last.

The first discriminator that recognises a message decides its type. If the type it finds isn't registered on the queue, the message is unroutable: it doesn't fall through to the next discriminator.

## Unroutable messages

A message that no registered type matches, for example because a producer started sending a new type before this consumer handles it, or a subject is misspelt, is **not deleted**. It's logged at `Error`, with the message id, the queue, what each discriminator found and the registered type names, and left on the queue. Once its retries are used up, the redrive policy moves it to the error queue, where you can redrive it from when a consumer can handle it.

On a queue with no error queue (`WithNoErrorQueue()`), an unroutable message is received again and again until the queue's retention period expires.

## Raw message delivery

`WithRawMessageDelivery()` declares that messages arrive as bare bodies, without an SNS notification or JustSaying's wrapper, and so without a `Subject`. A raw subscription can't route by subject, so it must give every type a name and add a discriminator that reads it from the body or attributes. A raw subscription that would route by subject throws when the bus is built.

```csharp
x.ForQueue("orders", q => q
    .WithRawMessageDelivery()
    .WithDiscriminator(new EventTypeAttributeDiscriminator())
    .Handling<OrderPlaced>("order-placed")
    .Handling<OrderCancelled>("order-cancelled"));
```

## Middleware and subscription groups

Middleware is configured per type, as the second argument of `Handling<T>`. As with `WithMiddlewareConfiguration`, the configuration replaces the default pipeline, so it must add the handler with `UseDefaults<T>`:

```csharp
x.ForQueue("orders", q => q
    .WithSubscriptionGroup("orders")
    .Handling<OrderPlaced>(middlewareConfiguration: m => m
        .UseExactlyOnce<OrderPlaced>("order-placed", deduplicationKeySelector: o => o.OrderId)
        .UseDefaults<OrderPlaced>(typeof(OrderPlacedHandler)))
    .Handling<OrderCancelled>());
```

`WithSubscriptionGroup` puts the queue in a [subscription group](../subscriptiongroups/), as for single-type subscriptions.
