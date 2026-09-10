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

## New extensibility seams

Available on `IMessagingConfig`:

- **`IMessageTypeRegistry`** — bidirectional map between a message type and its logical wire name (the SNS `Subject` today). `GetLogicalName` preserves existing subject behaviour; `TryResolveType` enables future type-based inbound routing. The native `Subject` remains the unqualified type name.
- **`IMessageMetadataProvider`** — reads the intrinsic id / timestamp / deduplication key a payload carries (mapping onto the CloudEvents `id`/`time`). Defaults to reading `Message` metadata. A custom provider feeds the message id on publish activities, publisher logs, the handling log middleware, and batch request entry ids.

Both have sensible defaults and require no action unless you are customising naming or metadata.
