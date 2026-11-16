---
---

# Consuming CloudEvents

You can handle a CloudEvent in two ways:

* as its **payload**, with a plain `IHandlerAsync<T>`. The envelope is removed before your handler is called, so CloudEvents doesn't appear in the handler's contract.
* as the **whole event**, with an `IHandlerAsync<CloudEvent<T>>`, to read its `Id`, `Source`, `Time`, `Subject`, `Extensions` and other attributes alongside its `Data`.

Each subscription states the CloudEvents `type` it reads, and only accepts events of that `type`.

## From a topic

`ForCloudEventTopicData<T>` handles the payload, and `ForCloudEventTopic<T>` the whole event:

```csharp
config.Subscriptions(x =>
{
    x.ForCloudEventTopicData<OrderPlaced>("com.example.order-placed");
    x.ForCloudEventTopic<OrderShipped>("com.example.order-shipped");
});

services.AddJustSayingHandler<OrderPlaced, OrderPlacedHandler>();
services.AddJustSayingHandler<CloudEvent<OrderShipped>, OrderShippedHandler>();
services.AddJustSayingCloudEvents();
```

```csharp
public class OrderShippedHandler : IHandlerAsync<CloudEvent<OrderShipped>>
{
    public Task<bool> Handle(CloudEvent<OrderShipped> message)
    {
        var tenant = message.Extensions.TryGetValue("tenantid", out var value) ? value : null;
        Console.WriteLine($"{message.Data.OrderId} shipped (event {message.Id} from {message.Source}, tenant {tenant})");
        return Task.FromResult(true);
    }
}
```

The topic and queue are named after `T`, as for `ForTopic<T>`, so they meet a `WithCloudEventTopic<T>` publication by default. Both take an optional callback with the same options as [`ForTopic<T>`](/subscriptions/configuration/fortopic), for example to name the topic and queue, or set a filter policy:

```csharp
x.ForCloudEventTopicData<OrderPlaced>("com.example.order-placed", c => c
    .WithTopicName("order-events")
    .WithQueueName("billing-order-placed"));
```

An event of another `type`, or one that isn't a valid CloudEvent, fails handling, and is retried and then moved to the error queue.

### Reading events of any `type`

To read every event on a topic as the same payload type, whatever its `type`, give a `ForTopic<T>` subscription a data-only serializer without a `type`, from the `CloudEventSerializationFactory` that `AddJustSayingCloudEvents` registers:

```csharp
services.AddJustSaying((config, serviceProvider) =>
{
    var cloudEvents = serviceProvider.GetRequiredService<CloudEventSerializationFactory>();

    config.Subscriptions(x => x.ForTopic<OrderAudit>(c => c
        .WithTopicName("order-events")
        .WithMessageBodySerializer(cloudEvents.GetDataOnlySerializer<OrderAudit>())));
});
```

Events are still validated, but any `type` is accepted. Passing a `type` to `GetDataOnlySerializer<T>(type)`, or mapping one with `MapType<T>`, restricts it to that `type`.

## Multi-type queues

On a [multi-type queue](/subscriptions/configuration/multi-type-queues), `HandlingCloudEventData<T>` and `HandlingCloudEvent<T>` register CloudEvents types, and they can sit alongside JustSaying's own messages:

```csharp
x.ForQueue("orders", q => q
    .Handling<OrderAccepted>()                                               // JustSaying format, routed by Subject
    .HandlingCloudEventData<OrderPlaced>("com.example.order-placed")         // handler receives OrderPlaced
    .HandlingCloudEvent<OrderShipped>("com.example.order-shipped"));         // handler receives CloudEvent<OrderShipped>
```

These route each message by its CloudEvents `type`. The `type` can be left out if it's [mapped](/cloudevents/publishing#the-type-map) with `CloudEventOptions.MapType<T>`.

A CloudEvent is always routed by its `type`, which is checked before the SNS `Subject` whatever order the types are registered in. A CloudEvent whose `type` isn't registered on the queue is unroutable: it's left for the redrive policy to move to the error queue, rather than being read as a JustSaying message.

A CloudEvent that arrives on a plain `ForTopic<T>` or `ForQueue<T>` subscription can't be read as `T`, so it fails handling with an error that tells you which CloudEvents subscription to use.

## Exactly-once handling

`CloudEvent<T>` doesn't derive from `Message`, so `UseExactlyOnce` needs a key selector. Use the event's `id`, which a producer keeps the same when it retries:

```csharp
x.ForCloudEventTopic<OrderShipped>("com.example.order-shipped", c => c
    .WithMiddlewareConfiguration(m => m
        .UseExactlyOnce<CloudEvent<OrderShipped>>("order-shipped-handler", deduplicationKeySelector: e => e.Id)
        .UseDefaults<CloudEvent<OrderShipped>>(typeof(OrderShippedHandler))));
```

A payload-only handler can't see the `id`, so handle the whole event if you need to deduplicate on it.

## Moving a message type to CloudEvents

To move a type from JustSaying's own format to CloudEvents without a flag day, have its consumers accept both before the producer changes. On a multi-type queue, register the type both ways:

```csharp
x.ForQueue("parcels", q => q
    .Handling<ParcelShipped>()                                                // the producer's current publications
    .HandlingCloudEventData<ParcelShipped>("com.example.parcel-shipped"));    // its CloudEvents ones
```

Both reach the same `IHandlerAsync<ParcelShipped>`. Once every consumer is deployed, change the producer from `WithTopic<ParcelShipped>()` to `WithCloudEventTopic<ParcelShipped>("com.example.parcel-shipped")` (or from `WithQueue<T>` to `WithCloudEventQueue<T>`). When it's the only format left on the queue, remove the `Handling<ParcelShipped>()` line.

A single-type subscription such as `ForTopic<T>` reads one format, so it can't accept both: use a multi-type queue for the migration, subscribed to the topic the type is published to:

```csharp
x.ForQueue("parcels", q => q
    .Handling<ParcelShipped>()
    .HandlingCloudEventData<ParcelShipped>("com.example.parcel-shipped")
    .SubscribeToTopic<ParcelShipped>());
```
