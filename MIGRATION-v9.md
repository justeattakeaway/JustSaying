# Migrating to JustSaying v9

> Draft — accumulates the breaking changes as the v9 work lands. Becomes the v9 release notes / docs migration page when v9 ships.

## Dropping the `Message` base-class constraint

The public APIs no longer require messages to derive from `JustSaying.Models.Message`. Every API that was `where T : Message` is now `where T : class`, so you can publish and handle any reference type.

- `Message` still exists and works unchanged — **no action needed** if your messages already derive from it.
- You may now use plain DTOs, records, or types from other libraries as messages.

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

The default message body serializer changes from **Newtonsoft.Json** to **System.Text.Json** (the source-generator-friendly path that enables Native AOT). This affects the default wire format — review for behavioural differences (for example, STScJ is stricter about types and handles some constructs differently).

To keep using Newtonsoft.Json, opt back in explicitly:

```csharp
services.AddJustSaying(...)
        // register the Newtonsoft factory before/after AddJustSaying as appropriate
```

…or via the fluent `WithMessageBodySerializer` / serialization-factory hook. Newtonsoft.Json remains fully supported as an opt-in; it is not Native-AOT-compatible.

### Serialization interface is generic

`IMessageBodySerializer` is now the generic `IMessageBodySerializer<T>` on the public surface (an internal type-erased seam handles the runtime boundary). If you implement a custom serializer or serialization factory, update to the generic signatures. Routing and serialization remain by each message's runtime type, as in v8: a single (or batch) publish of a base-typed instance is still routed to, and serialized by, the publisher registered for its concrete type.

## Exactly-once handling requires a stable key for non-`Message` payloads

`UseExactlyOnce<TMessage>` previously deduplicated on `Message.UniqueKey()`, falling back to a fresh GUID per receive for anything else — which silently turned exactly-once into a no-op. v9 fails fast instead:

```csharp
// Message-derived types: unchanged, uses Message.UniqueKey()
pipeline.UseExactlyOnce<OrderAccepted>("orders-handler");

// Non-Message types: provide a stable deduplication key, or registration throws
pipeline.UseExactlyOnce<OrderPlaced>("orders-handler",
    deduplicationKeySelector: m => m.OrderRef);
```

If a non-`Message` type is used without a `deduplicationKeySelector`, `UseExactlyOnce` throws at registration (startup) rather than degrading silently at runtime.

## One publication per message type

Registering two publications for the same message type (for example `WithTopic<Order>()` twice, or a
`WithTopic<Order>()` alongside a `WithQueue<Order>()`) previously last-write-wins: the earlier
registration was silently discarded. v9 throws at startup instead:

> A publisher for message type 'Order' is already registered. Each message type can only have one publication.

If you hit this, remove the redundant registration — only one of them was ever taking effect.

## New extensibility seams

Available on `IMessagingConfig`:

- **`IMessageTypeRegistry`** — bidirectional map between a message type and its logical wire name (the SNS `Subject` today). `GetLogicalName` preserves existing subject behaviour; `TryResolveType` enables future type-based inbound routing. The native `Subject` remains the unqualified type name.
- **`IMessageMetadataProvider`** — reads the intrinsic id / timestamp / deduplication key a payload carries (mapping onto the CloudEvents `id`/`time`). Defaults to reading `Message` metadata.

Both have sensible defaults and require no action unless you are customising naming or metadata.
