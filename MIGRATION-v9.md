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

## CloudEvents (new package: `JustSaying.CloudEvents`)

v9 can publish and consume [CloudEvents 1.0](https://github.com/cloudevents/spec) structured-mode envelopes via the new `JustSaying.CloudEvents` package. The envelope is chosen **per registration**, not per application: `services.AddJustSayingCloudEvents(...)` registers the CloudEvents serializer as its own service and leaves the app-wide default serializer untouched, so legacy, plain-JSON and CloudEvents registrations coexist in one app.

```csharp
services.AddJustSayingCloudEvents();

// publications — only the CloudEvents registration writes CloudEvents
p.WithTopic<OrderPlaced>();                                       // legacy (Message-derived)
p.WithTopic<PaymentTaken>();                                      // plain JSON POCO
p.WithCloudEvent<ParcelShipped>("com.example.parcel-shipped",
    source: new Uri("https://orders.example.com"));               // CloudEvents

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
    options.WithCloudEventType<OrderPlaced>("com.example.order-placed");
},
useAsDefault: true);
```

Single-type subscriptions can also override their serializer per registration via `WithMessageBodySerializer(IMessageBodySerializer<T>)`, now available on the `ForTopic<T>`/`ForQueue<T>` builders as well as `ForQueueUrl<T>`/`ForQueueArn<T>`.

## New extensibility seams

Available on `IMessagingConfig`:

- **`IMessageTypeRegistry`** — bidirectional map between a message type and its logical wire name (the SNS `Subject` today). `GetLogicalName` preserves existing subject behaviour; `TryResolveType` enables future type-based inbound routing. The native `Subject` remains the unqualified type name.
- **`IMessageMetadataProvider`** — reads the intrinsic id / timestamp / deduplication key a payload carries (mapping onto the CloudEvents `id`/`time`). Defaults to reading `Message` metadata. A custom provider feeds the message id on publish activities, publisher logs, the handling log middleware, and batch request entry ids.

Both have sensible defaults and require no action unless you are customising naming or metadata.
