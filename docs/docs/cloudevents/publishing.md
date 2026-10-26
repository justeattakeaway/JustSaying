---
---

# Publishing CloudEvents

Register a CloudEvents publication with `WithCloudEventTopic<T>` for a topic, or `WithCloudEventQueue<T>` for a queue, stating the event's `type`:

```csharp
config.Publications(x =>
{
    x.WithCloudEventTopic<OrderPlaced>("com.example.order-placed",
        source: new Uri("/orders", UriKind.Relative));

    x.WithCloudEventQueue<RefundRequested>("com.example.refund-requested",
        source: new Uri("/orders", UriKind.Relative));
});
```

Then publish as usual:

```csharp
await publisher.PublishAsync(new OrderPlaced("order-1", 12.5m));
```

The topic and queue are named by the naming conventions applied to `T` (here `orderplaced` and `refundrequested`), as for `WithTopic<T>` and `WithQueue<T>`. Pass a [destination](/destinations/) to name them, configure how they're created, or use an existing one:

```csharp
x.WithCloudEventTopic<OrderPlaced>(
    TopicDestination.FromArn("arn:aws:sns:eu-west-1:111122223333:order-events"),
    "com.example.order-placed",
    new Uri("/orders", UriKind.Relative));

x.WithCloudEventQueue<RefundRequested>(
    QueueDestination.Named("refunds", q => q.WithVisibilityTimeout(TimeSpan.FromMinutes(1))),
    "com.example.refund-requested");
```

When a registration has no `source`, it uses `CloudEventOptions.Source`. One of the two must be set, or the bus throws when it's built.

## Publishing the payload or the whole event

Each CloudEvents registration accepts two shapes of message, which go to the same topic or queue:

* **The payload, `T`.** JustSaying fills in the envelope: the `type` and `source` from the registration, and a new `id` and `time`. If `T` derives from `Message`, its `Id` and `TimeStamp` are used instead.
* **A `CloudEvent<T>`.** You control the envelope for each message: `id`, `source`, `time`, `subject`, `dataschema`, `datacontenttype` and extension attributes, and even `type`. Anything you leave out is filled in as for a bare `T`.

```csharp
await publisher.PublishAsync(new CloudEvent<OrderPlaced>(
    new OrderPlaced("order-2", 30m),
    subject: "orders/order-2",
    extensions: new Dictionary<string, string> { ["tenantid"] = "acme" }));
```

The `id` and `time` JustSaying creates are fixed for each message instance, so publish retries send the same `id` and consumers can tell a retry is a duplicate.

## Configuring the publication

Both methods take an optional `configure` callback, the same `TopicPublicationBuilder<T>` or `QueuePublicationBuilder<T>` as `WithTopic<T>` and `WithQueue<T>`. It applies to both shapes, and a callback that receives a message (an exception handler, or a topic name function) receives the payload, `CloudEvent<T>.Data`:

```csharp
x.WithCloudEventTopic<OrderPlaced>("com.example.order-placed",
    configure: cfg => cfg.WithExceptionHandler((Exception exception, OrderPlaced order) =>
    {
        logger.LogError(exception, "Couldn't publish {OrderId}", order.OrderId);
        return false;
    }));
```

CloudEvents publications are never compressed, and configuring compression on one throws.

## The `type` map

Rather than repeating each `type` at the registration, you can map it once on `CloudEventOptions`. Subscriptions use the same map:

```csharp
services.AddJustSayingCloudEvents(options =>
{
    options.Source = new Uri("/orders", UriKind.Relative);
    options.MapType<OrderPlaced>("com.example.order-placed");
});
```

`WithCloudEventTopic<T>` and `WithCloudEventQueue<T>` always take a `type`, which is the one written. The map is used by `HandlingCloudEvent<T>()`, `HandlingCloudEventData<T>()` without a name, and by every registration when CloudEvents is the [default format](/cloudevents/#an-all-cloudevents-application).

## Rules

* Each message type has one publication, so `T` can't also have a `WithTopic<T>` or `WithQueue<T>` publication.
* Extension names must be lowercase letters and digits only. `CloudEvent<T>` throws for any other name, or for one of the CloudEvents attributes (such as `subject`).
* `data` can't be `null`.
