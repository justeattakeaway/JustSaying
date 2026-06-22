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

### Serialization is generic; polymorphism is explicit

`IMessageBodySerializer` is now the generic `IMessageBodySerializer<T>` on the public surface (an internal type-erased seam handles the runtime boundary). Serialization is by the **declared type `T`**, not the runtime type.

- If you relied on derived-type properties being serialized when publishing a base type, opt in with System.Text.Json polymorphism (`[JsonPolymorphic]` / `[JsonDerivedType]`). The previous implicit behaviour was already lossy on the receive side; the attributes round-trip a discriminator correctly.

### Routing by declared type; homogeneous batches

- Publishing routes by `typeof(T)` (the static type), not the runtime type of the instance.
- A single `PublishBatchAsync<T>` call publishes one message type; mixed-type batches are no longer split by runtime type within one call.

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

## New extensibility seams

Available on `IMessagingConfig`:

- **`IMessageTypeRegistry`** — bidirectional map between a message type and its logical wire name (the SNS `Subject` today). `GetLogicalName` preserves existing subject behaviour; `TryResolveType` enables future type-based inbound routing. The native `Subject` remains the unqualified type name.
- **`IMessageMetadataProvider`** — reads the intrinsic id / timestamp / deduplication key a payload carries (mapping onto the CloudEvents `id`/`time`). Defaults to reading `Message` metadata.

Both have sensible defaults and require no action unless you are customising naming or metadata.
