---
---

# Message Types

A message is any class or record. JustSaying serializes it to JSON when you publish it, and deserializes it back into the same type before calling your handler.

```csharp
public record OrderPlaced(string OrderId, decimal Total);

public class OrderReadyEvent
{
    public int OrderId { get; set; }
}
```

Messages must be reference types (`where T : class`), so structs and `record struct`s can't be used. The serialized message, plus anything JustSaying adds around it, must fit within the SNS/SQS limit of 256KB.

## The `Message` base class

Before v9, every message had to derive from `JustSaying.Models.Message`. That's now optional, and a message that does derive from it works exactly as before. `Message` gives a message:

| Member | Description |
| --- | --- |
| `Id` | A `Guid` assigned when the message is created. Logged when the message is published and handled, and used as the `messaging.message.id` tag on the publish activity. |
| `TimeStamp` | The UTC time the message was created. |
| `RaisingComponent`, `Tenant`, `Conversation` | Optional properties for your own use. |
| `UniqueKey()` | The key [exactly-once handling](#exactly-once-handling) deduplicates on. Defaults to `Id`. |

A message that doesn't derive from `Message` has no id JustSaying can read, so logs show its id as `(null)` and the publish activity has no `messaging.message.id` tag. Add your own id property if you need one.

## Publishing by runtime type

A message is routed and serialized by its **runtime** type, not the type it's declared as at the call site. Publishing a message through a base-typed variable sends it to the publication registered for its concrete type:

```csharp
public abstract record OrderEvent(string OrderId);
public record OrderPlaced(string OrderId) : OrderEvent(OrderId);
public record OrderCancelled(string OrderId) : OrderEvent(OrderId);

config.Publications(x =>
{
    x.WithTopic<OrderPlaced>();
    x.WithTopic<OrderCancelled>();
});

// Goes to the 'orderplaced' topic, serialized as an OrderPlaced.
OrderEvent message = new OrderPlaced("order-1");
await publisher.PublishAsync(message);
```

This means:

* Register a publication for each type you publish.
* Each message type can only have one publication. Registering a second one for the same type (for example `WithTopic<Order>()` and `WithQueue<Order>()`) throws when the bus is built.
* Publishing a type with no publication throws an `InvalidOperationException`. If you pass a collection to `PublishAsync`, the exception tells you to call [`PublishBatchAsync`](/publishing/batch-publishing) instead.

## Naming

Topic and queue names come from the message type's name through the [naming conventions](/messaging-configuration/naming-conventions) (by default the lowercased type name, so `OrderPlaced` uses `orderplaced`), unless you name the [destination](/destinations) explicitly.

When publishing to SNS, JustSaying sets the message's `Subject` to its unqualified type name (`OrderPlaced`). Subscribers use the `Subject` to tell message types apart on a [queue that carries several types](/subscriptions/configuration/multi-type-queues). You can change it with `WithMessageSubjectProvider` in the [messaging configuration](/messaging-configuration/), but every consumer then has to use the same provider.

## Exactly-once handling

`UseExactlyOnce` stops a message being handled twice by taking a lock on a key for each message. For a type that derives from `Message`, the key is `Message.UniqueKey()`. Any other type has no such key, so you must say what it is:

```csharp
x.ForTopic<OrderPlaced>(c => c
    .WithMiddlewareConfiguration(m => m
        .UseExactlyOnce<OrderPlaced>("order-placed-handler",
            deduplicationKeySelector: message => message.OrderId)
        .UseDefaults<OrderPlaced>(typeof(OrderPlacedHandler))));
```

* Without a `deduplicationKeySelector`, `UseExactlyOnce` throws for a type that doesn't derive from `Message`, when the bus is built.
* The key should be stable across redeliveries and publish retries, so use an id the producer assigns, not one created when the message is handled.
* If the selector returns `null` or whitespace for a message, that message isn't handled: an error is logged and the message stays on the queue for its redrive policy.
* `UseExactlyOnce` needs an `IMessageLockAsync` registered in the container.

See [Middleware](/subscriptions/middleware/) for how the middleware pipeline is put together.

## Serialization

Messages are serialized with System.Text.Json by default. See [Serialization](/messages/serialization) for the defaults and how to change them.
