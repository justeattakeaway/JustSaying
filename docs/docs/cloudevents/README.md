---
---

# CloudEvents

[CloudEvents](https://cloudevents.io/) is a vendor-neutral format for describing events. The `JustSaying.CloudEvents` package publishes and consumes CloudEvents 1.0 over SNS and SQS, so JustSaying can exchange events with services that don't use JustSaying, such as ones built on AWS.Messaging, the CNCF CloudEvents SDKs or Brighter.

```bash
dotnet add package JustSaying.CloudEvents
```

A CloudEvent wraps your message in an envelope of attributes:

```json
{
  "specversion": "1.0",
  "id": "4a3c2b1e-6f4f-4f1c-9a43-2a6f2d6c5e70",
  "source": "/orders",
  "type": "com.example.order-placed",
  "time": "2026-10-01T09:30:00+00:00",
  "datacontenttype": "application/json",
  "data": { "OrderId": "order-1", "Total": 12.5 }
}
```

## Setup

Call `AddJustSayingCloudEvents` alongside `AddJustSaying`, then opt individual registrations in to CloudEvents:

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("eu-west-1"));

    config.Publications(x =>
    {
        x.WithTopic<OrderAccepted>();                           // JustSaying's own format
        x.WithCloudEventTopic<OrderPlaced>("com.example.order-placed",
            source: new Uri("/orders", UriKind.Relative));      // CloudEvents
    });
});

services.AddJustSayingCloudEvents();
```

The format is chosen **per registration**. `AddJustSayingCloudEvents` doesn't change how the app's other messages are written, so JustSaying's own format, plain JSON and CloudEvents can all be used in one application.

* [Publishing CloudEvents](publishing.md): `WithCloudEventTopic<T>` and `WithCloudEventQueue<T>`.
* [Consuming CloudEvents](consuming.md): `ForCloudEventTopic<T>`, `ForCloudEventTopicData<T>`, `HandlingCloudEvent<T>` and `HandlingCloudEventData<T>`.

## Options

`AddJustSayingCloudEvents` takes an optional callback to configure `CloudEventOptions`:

| Option | Description |
| --- | --- |
| `Source` | The `source` for events published without one of their own. A publication can set its own instead. Only needed to publish. |
| `MapType<T>(type)` | The `type` for a message type, so registrations don't have to repeat it. |
| `DataContentType` | The `datacontenttype` written for the `data`. Defaults to `application/json`, and must be a JSON media type. |
| `DataSerializationFactory` | The serializer for the `data` payload. By default it's the app's own `IMessageBodySerializationFactory`, so `data` is written with the same JSON settings as the app's other messages. |
| `UseAsDefault` | Make CloudEvents the format for *every* registration. See [below](#an-all-cloudevents-application). |

```csharp
services.AddJustSayingCloudEvents(options =>
{
    options.Source = new Uri("/orders", UriKind.Relative);
    options.MapType<OrderPlaced>("com.example.order-placed");
});
```

`AddJustSayingCloudEvents` can be called more than once, for example by a library that maps its own types. Each call's callback configures the same options, in the order they're called.

Prefer a **relative** `source`, such as `/orders`. Some consumers, AWS.Messaging among them, reject an absolute URI. The value is written exactly as you give it.

## An all-CloudEvents application

With `UseAsDefault`, CloudEvents replaces the app-wide serializer, so plain `WithTopic<T>`, `ForTopic<T>` and `ForQueue<T>` registrations read and write CloudEvents too. Every type the app publishes then needs a `type`, mapped with `MapType`, or the bus throws when it's built. `Source` is only needed to publish: a consume-only app can leave it out, and an app that publishes without one fails at its first publish.

```csharp
services.AddJustSayingCloudEvents(options =>
{
    options.Source = new Uri("/orders", UriKind.Relative);
    options.MapType<OrderPlaced>("com.example.order-placed");
    options.MapType<OrderShipped>("com.example.order-shipped");
    options.UseAsDefault = true;
});
```

Once a call has turned `UseAsDefault` on, a later call can't turn it off again: that throws. Plain registrations of the envelope itself, such as `ForTopic<CloudEvent<T>>`, aren't supported this way and fail at startup; use `ForCloudEventTopic<T>`, `HandlingCloudEvent<T>` or `WithCloudEventTopic<T>`.

## What's supported

* **Structured mode only.** The whole event is the message body (`application/cloudevents+json`). Binary mode, where the attributes travel as SNS or SQS message attributes, isn't written or read.
* **One event per message.** The batch format (`application/cloudevents-batch+json`) isn't supported. `PublishBatchAsync` works, but sends each event as its own message.
* **JSON data.** `data` is written as JSON. When reading, JSON `data` is read inline, and JSON sent as `data_base64` is decoded and read the same way. An event with another media type, with both `data` and `data_base64`, or with no data at all fails handling.
* **Validated on read.** An event must have `specversion` `1.0`, a non-empty `id` and `type`, and a `source` that's a URI reference, and a `time`, if present, must be an RFC 3339 timestamp with an offset. A subscription that knows its `type` only reads events of that `type`. This applies whether the handler receives the payload or the whole event.
* **Extensions** must be named with lowercase letters and digits only (`tenantid`, not `TenantId` or `tenant-id`). Extension values are strings: an integer or boolean extension that's received is kept as its JSON text (`"42"`, `"true"`).
* **No compression.** CloudEvents publications are never compressed, because other consumers couldn't read them. Configuring compression on one throws when the bus is built.

An event that can't be read fails handling: it's retried, then moved to the error queue. It's never passed to a handler half-populated.

## Interoperability

* **SNS raw message delivery.** A consumer that doesn't use JustSaying should subscribe its queue to the topic with SNS `RawMessageDelivery` enabled, so it receives the CloudEvent itself rather than the SNS notification around it. JustSaying subscribers can read either.
* **Queues.** A CloudEvent published to a queue is sent as it is, without JustSaying's `{ "Subject", "Message" }` wrapper.
* **SNS `Subject`.** A CloudEvents topic publication also sets the SNS `Subject` to the payload type's name, but JustSaying always routes a CloudEvent by its `type`.
* **AsyncAPI.** [AsyncAPI documents](/asyncapi/) describe CloudEvents registrations, including their `type` and payload schema.
* **Native AOT.** The `data` is written with the app's own serializer factory, so the source-generated `JsonSerializerContext` you register for [Native AOT](/advanced/native-aot) covers CloudEvents payloads too.
