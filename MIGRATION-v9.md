# Migrating to JustSaying v9

> Draft — accumulates the breaking changes as the v9 work lands. Becomes the v9 release notes / docs migration page when v9 ships.

## Dropping the `Message` base-class constraint

The public APIs no longer require messages to derive from `JustSaying.Models.Message`. Every API that was `where T : Message` is now `where T : class`, so you can publish and handle any reference type.

- `Message` still exists and works unchanged, and message classes that derive from it need no changes.
- You may now use plain DTOs, records, or types from other libraries as messages.
- This includes the container registration helpers: `AddJustSayingHandler<TMessage, THandler>` and `AddJustSayingHandlers<TMessage>` (Microsoft DI) and `Registry.AddJustSayingHandler<TMessage, THandler>` (StructureMap) are now `where TMessage : class`.

### Extension points that took a `Message` now take an `object`

Widening the constraint means the *framework's* callbacks can no longer promise a `Message`. If you implement or configure any of these, the signature has changed and your code needs a source update — even if all your messages still derive from `Message`:

| Extension point | v8 | v9 |
| --- | --- | --- |
| `IMessageMonitor.Handled` | `Handled(Message message)` | `Handled(object message)` |
| `IMessageBackoffStrategy.GetBackoffDuration` | `(Message message, int approximateReceiveCount, Exception lastException = null)` | `(object message, int approximateReceiveCount, Exception lastException = null)` |
| `IPublishConfiguration.MessageResponseLogger` (and `MessagingConfigurationBuilder.WithMessageResponseLogger`) | `Action<MessageResponse, Message>` | `Action<MessageResponse, object>` |
| `IPublishBatchConfiguration.MessageBatchResponseLogger` (and `MessagingConfigurationBuilder.WithMessageResponseLogger`) | `Action<MessageBatchResponse, IReadOnlyCollection<Message>>` | `Action<MessageBatchResponse, IReadOnlyCollection<object>>` |
| `SnsWriteConfiguration.HandleException` / `SnsWriteConfigurationBuilder.WithErrorHandler` | `Func<Exception, Message, bool>` | `Func<Exception, object, bool>` |
| `TopicAddressPublicationBuilder<T>.WithExceptionHandler` / `WithTopicAddress` | `Message`-typed delegates | `T`-typed delegates |
| `TopicPublicationBuilder<T>.WithTopicName` | `Func<Message, string>` | `Func<T, string>` |

The per-publication builders are now typed on their own `T` rather than `object`, so those delegates get *more* specific: a `Func<Exception, OrderPlaced, bool>` no longer needs a cast.

Implementations usually just need the parameter type widening; where you relied on `Message` members, pattern match first:

```csharp
public void Handled(object message)
{
    if (message is Message typed)
    {
        _metrics.Record(typed.Id);
    }
}
```

Explicitly typed lambdas need the same treatment — `(Exception ex, Message m) => ...` becomes `(Exception ex, object m) => ...`. Lambdas written with inferred parameters (`(ex, m) => ...`) continue to compile unchanged.

### Batch publishing is renamed to `PublishBatchAsync`

The biggest source-breaking change. Because a `List<T>` is itself an `object`/`class`, a single generic `PublishAsync<T>` would silently bind a collection to the single-message overload. Batch publishing therefore has a distinct verb:

```csharp
// Before
await publisher.PublishAsync(messages, metadata, cancellationToken);

// After
await publisher.PublishBatchAsync(messages, metadata, cancellationToken);
```

Rename batch calls accordingly. Single-message `PublishAsync` is unchanged.

### The default serializer is now System.Text.Json

The default message body serializer changes from **Newtonsoft.Json** to **System.Text.Json** (the source-generator-friendly path that enables Native AOT). This affects the default wire format — review for behavioural differences (for example, STJ is stricter about types and handles some constructs differently).

To keep using Newtonsoft.Json, register the factory yourself. `AddJustSaying` registers the System.Text.Json factory with `TryAddSingleton`, so **register yours before the `AddJustSaying` call** and it wins:

```csharp
using JustSaying.Messaging.MessageSerialization;

// Must come before AddJustSaying — the default is registered with TryAddSingleton.
services.AddSingleton<IMessageBodySerializationFactory>(
    new NewtonsoftSerializationFactory());

services.AddJustSaying(builder => builder.Messaging(c => c.WithRegion("eu-west-1")));
```

Pass `JsonSerializerSettings` to the constructor if you were customising them:

```csharp
services.AddSingleton<IMessageBodySerializationFactory>(
    new NewtonsoftSerializationFactory(new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
    }));
```

StructureMap resolves the *last* registration rather than the first, so there register it **after** `AddJustSaying`:

```csharp
var container = new Container(registry =>
{
    registry.AddJustSaying("eu-west-1");
    registry.For<IMessageBodySerializationFactory>()
            .Use(new NewtonsoftSerializationFactory())
            .Singleton();
});
```

Newtonsoft.Json remains fully supported as an opt-in. It is not Native-AOT-compatible, so `NewtonsoftSerializationFactory` is annotated with `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` and will produce trim/AOT warnings in a project that opts into those analysers.

### Serialization interface is generic

`IMessageBodySerializer` is now the generic `IMessageBodySerializer<T>` on the public surface (an internal type-erased seam handles the runtime boundary). If you implement a custom serializer or serialization factory, update to the generic signatures. Routing and serialization remain by each message's runtime type, as in v8: a single (or batch) publish of a base-typed instance is still routed to, and serialized by, the publisher registered for its concrete type.

Because of that, `T` is always the concrete published type in JustSaying's own path. If you construct a serializer for a base type yourself, note that the two built-in implementations differ: `SystemTextJsonMessageBodySerializer<T>` serializes the declared type `T` and omits derived-only members, while `NewtonsoftMessageBodySerializer<T>` builds its contract from the runtime type and includes them.

## Exactly-once handling requires a stable key for non-`Message` payloads

`UseExactlyOnce<TMessage>` previously deduplicated on `Message.UniqueKey()`, falling back to a fresh GUID per receive for anything else — which silently turned exactly-once into a no-op. v9 fails fast instead:

```csharp
// Message-derived types: unchanged, uses Message.UniqueKey()
pipeline.UseExactlyOnce<OrderAccepted>("orders-handler");

// Non-Message types: provide a stable deduplication key, or registration throws
pipeline.UseExactlyOnce<OrderPlaced>("orders-handler",
    deduplicationKeySelector: m => m.OrderRef);
```

If a non-`Message` type is used without a `deduplicationKeySelector`, `UseExactlyOnce` throws at registration (startup) rather than degrading silently at runtime. A selector that returns null or whitespace for a given message also throws when that message is handled, rather than collapsing unrelated messages onto a shared lock key.

## One publication per message type

Registering two publications for the same message type (for example `WithTopic<Order>()` twice, or a
`WithTopic<Order>()` alongside a `WithQueue<Order>()`) previously last-write-wins: the earlier
registration was silently discarded. v9 throws at startup instead:

> A publisher for message type 'Order' is already registered. Each message type can only have one publication.

If you hit this, remove the redundant registration — only one of them was ever taking effect.

## Destinations are values: `Topic` and `Queue`

The fluent registration API is rebuilt around two destination values. A `Topic` or `Queue` says
*which* resource a registration targets and — when JustSaying owns it — *how to create it*; the
registration builders configure publish/read-time behaviour only, and are the same type whether
the resource is created by JustSaying or already exists:

```csharp
p.WithTopic<OrderPlaced>();                                   // by convention, created
p.WithTopic<OrderPlaced>(Topic.Named("orders"));              // by name, created
p.WithTopic<OrderPlaced>(Topic.Named("orders", t => t
    .WithTag("team", "payments")
    .WithEncryption(masterKeyId)));                           // creation config lives on the value
p.WithTopic<OrderPlaced>(Topic.FromArn(topicArn));            // pre-existing, never created —
                                                              // no creation config to mis-set

s.ForQueue<Refund>(Queue.Named("refunds", q => q
    .WithMessageRetention(TimeSpan.FromDays(4))
    .WithNoErrorQueue()));
s.ForQueue<Refund>(Queue.FromUri(queueUri));
s.ForQueue(Queue.FromUri(queueUri), q => q                    // multi-type over an existing queue
    .Handling<OrderPlaced>()
    .HandlingCloudEvent<ParcelShipped>("com.example.parcel-shipped"));

s.ForTopic<OrderPlaced>(cfg => cfg
    .WithQueue(Queue.Named("orders-sub", q => q.WithTag("team", "payments")))
    .WithFilterPolicy(filterPolicyJson)
    .WithSubscriptionGroup("orders"));
```

This restructures the v8 fluent surface:

- **Removed:** `WithWriteConfiguration(...)`, `WithReadConfiguration(...)`, `WithTag(...)` on the
  registration builders, the `SnsWriteConfigurationBuilder`/`SqsWriteConfigurationBuilder`/
  `SqsReadConfigurationBuilder` wrappers, and the `TopicAddressPublicationBuilder`/
  `QueueAddressPublicationBuilder`/`QueueAddressSubscriptionBuilder` classes.
- **Where each knob went:** queue/topic creation settings (retention, visibility timeout,
  delivery delay, error-queue settings, encryption, tags) → `Topic.Named`/`Queue.Named`/
  `*.ByConvention` configuration; publish-time settings → the builder (`WithSubject`,
  `WithCompression`, `WithRawMessages`, `WithExceptionHandler`); subscription settings on
  `ForTopic` → the builder (`WithRawMessageDelivery`, `WithFilterPolicy`,
  `WithTopicSourceAccount`); read-time settings → the builder (`WithSubscriptionGroup`,
  `WithRawMessageDelivery`).
- **Kept:** `WithTopicArn<T>`, `WithQueueArn/Url/Uri<T>`, `ForQueueArn/Url/Uri<T>` remain, now
  delegating to the unified methods; their configure lambdas are retyped to the merged builders,
  which carry every member the old address builders had — most v8 call sites recompile unchanged.
- **Now works everywhere:** publish exception handlers apply to created topics too (previously
  the fluent create path silently dropped them), and compression consistently falls back to the
  bus-wide default options in every mode.

The CloudEvents registrations take the same values, so they never need per-address variants:

```csharp
p.WithCloudEventTopic<ParcelShipped>(Topic.FromArn(topicArn),
    "com.example.parcel-shipped", source);
p.WithCloudEventQueue<OrderCancelled>(Queue.FromUrl(queueUrl),
    "com.example.order-cancelled", source);
```

## CloudEvents (new package: `JustSaying.CloudEvents`)

v9 can publish and consume [CloudEvents 1.0](https://github.com/cloudevents/spec) structured-mode envelopes via the new `JustSaying.CloudEvents` package. The envelope is chosen **per registration**, not per application: `services.AddJustSayingCloudEvents(...)` registers the CloudEvents serializer as its own service and leaves the app-wide default serializer untouched, so legacy, plain-JSON and CloudEvents registrations coexist in one app.

```csharp
services.AddJustSayingCloudEvents();

// publications — only the CloudEvents registration writes CloudEvents
p.WithTopic<OrderPlaced>();                                       // legacy (Message-derived)
p.WithTopic<PaymentTaken>();                                      // plain JSON POCO
p.WithCloudEventTopic<ParcelShipped>("com.example.parcel-shipped",
    source: new Uri("https://orders.example.com"));               // CloudEvents

// point-to-point queue publications have a matching registration; the CloudEvents
// serializer is self-describing, so the envelope goes to the queue verbatim
// (no { "Subject", "Message" } wrapper)
p.WithCloudEventQueue<OrderCancelled>("com.example.order-cancelled",
    source: new Uri("https://orders.example.com"));

// subscriptions — one queue can mix native and CloudEvents messages
s.ForQueue("orders", q => q
    .Handling<LegacyOrderPlaced>()                                // native, routed by Subject
    .HandlingCloudEvent<ParcelShipped>("com.example.parcel-shipped")   // handler receives CloudEvent<T>
    .HandlingCloudEventData<OrderCancelled>("com.example.order-cancelled")); // handler receives bare T
```

For an all-CloudEvents application, opt the CloudEvents serializer in as the app-wide default — then plain `WithTopic<T>`/`ForQueue<T>` registrations speak CloudEvents too, and every published type must have a `type` mapped in `CloudEventOptions` (an unmapped type fails at startup):

```csharp
services.AddJustSayingCloudEvents(options =>
{
    options.Source = new Uri("https://orders.example.com");
    options.MapType<OrderPlaced>("com.example.order-placed");
},
useAsDefault: true);
```

Single-type subscriptions can also override their serializer per registration via `WithMessageBodySerializer(IMessageBodySerializer<T>)`, now available on the `ForTopic<T>`/`ForQueue<T>` builders as well as `ForQueueUrl<T>`/`ForQueueArn<T>`.

## New extensibility seams

Available on `IMessagingConfig`:

- **`IMessageTypeRegistry`** — bidirectional map between a message type and its logical wire name (the SNS `Subject` today). `GetLogicalName` preserves existing subject behaviour; `TryResolveType` enables future type-based inbound routing. The native `Subject` remains the unqualified type name.
- **`IMessageMetadataProvider`** — reads the intrinsic id / timestamp / deduplication key a payload carries (mapping onto the CloudEvents `id`/`time`). Defaults to reading `Message` metadata. A custom provider feeds the message id on publish activities, publisher logs, the handling log middleware, and batch request entry ids.

Both have sensible defaults and require no action unless you are customising naming or metadata.
