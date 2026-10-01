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

### Middleware and serialization contexts carry the message as `object`

For the same reason, these members are now typed as `object` instead of `Message`. Custom middleware that reads `context.Message.Id`, and code that constructs these types (for example in tests), needs updating:

| Type | Changed members |
| --- | --- |
| `HandleMessageContext` | `Message` property and the constructor's `message` parameter |
| `PublishContext` | `Message` and `Messages` properties, and both constructors |
| `InboundMessage` | `Message` property, the constructor and `Deconstruct` |

In handle middleware, `context.MessageAs<OrderPlaced>()` returns the message typed, or `null` if it's another type. Elsewhere, pattern match as above.

### Other public API changes

- `IMessagePublisher.PublishAsync` and `IMessageBatchPublisher.PublishBatchAsync` are now generic methods (`PublishAsync<TMessage>(TMessage message, ...) where TMessage : class`). Single-message call sites compile unchanged, and NSubstitute assertions such as `Received().PublishAsync(Arg.Any<Message>(), ...)` still match, but hand-written implementations (for example fake publishers in test suites) must implement the generic signatures.
- `IMessagingConfig` has a new member, `MessageTypeRegistry`, so your own implementations of the interface must add it. `MessagingConfig` already has it.
- The `ExactlyOnceMiddleware<T>` constructor has a new `deduplicationKeySelector` parameter (`Func<T, string>`) before `logger`. Prefer `UseExactlyOnce<T>` to constructing it directly.
- Batch publish middleware runs once per message type in the batch. In v8 a batch ran the middleware once, chosen by the type of the first message, with every message in the context. v9 groups a batch by each message's runtime type and runs each group through the middleware registered for that type, with only that group's messages in `PublishContext.Messages`. The groups run concurrently and share the `PublishBatchMetadata` you passed in, so middleware that writes to the metadata for one group affects the others.

### Batch publishing is renamed to `PublishBatchAsync`

**Your v8 batch calls still compile, then fail at runtime.** A `List<T>` is itself a `class`, so `PublishAsync(messages)` now binds to the single-message `PublishAsync<TMessage>` and treats the whole list as one message. No publisher is registered for `List<T>`, so the call throws an `InvalidOperationException` that tells you to call `PublishBatchAsync`. The compiler won't find these call sites for you: search your code for `PublishAsync` calls that pass a collection. Batch publishing has its own method name:

```csharp
// Before
await publisher.PublishAsync(messages, metadata, cancellationToken);

// After
await publisher.PublishBatchAsync(messages, metadata, cancellationToken);
```

Rename batch calls accordingly. Single-message `PublishAsync` call sites are unchanged.

### The default serializer is now System.Text.Json

The default message body serializer changes from **Newtonsoft.Json** to **System.Text.Json** (STJ), which can run under Native AOT. The defaults (`SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions`) bind like v8 where STJ allows it: property names are read case-insensitively, public fields are included, get-only collections are populated, and non-ASCII and HTML-sensitive characters aren't escaped. The rest of this section is what still differs.

If you customise the options, start from a copy of the defaults so you keep that behaviour:

```csharp
var options = new JsonSerializerOptions(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions)
{
    // your changes
};

services.AddSingleton<IMessageBodySerializationFactory>(new SystemTextJsonSerializationFactory(options));
```

#### What changes on the wire

Released v8 used Newtonsoft.Json's out-of-the-box settings. By default v9 writes:

- **Enums as strings** (`"Status":"Paid"`) where v8 wrote numbers (`"Status":1`). This applies to the STJ default and to the v9 Newtonsoft opt-in (`new NewtonsoftSerializationFactory()`). Both still read numbers, and v8 reads the strings, so services interoperate. But **SNS filter policies that match an enum's number in the message body stop matching**. Update them before a v9 producer goes live.
- **No `null` properties.** v8 wrote `"Note":null`; v9 leaves the property out (both serializers). Consumers end up with the same value, but a body filter policy that matches `null` changes.
- Whole-number `double` values as `1` rather than `1.0` (STJ only).

For byte-for-byte the v8 wire format, see [Keeping Newtonsoft.Json](#keeping-newtonsoftjson).

#### System.Text.Json vs Newtonsoft.Json checklist

Check each message type for these. None of them fail at startup, and most lose data silently:

- **Newtonsoft attributes are ignored.** A `[Newtonsoft.Json.JsonProperty("customer_id")]` name is lost in both directions: the property is written as `CustomerId`, and `customer_id` from other services isn't read. `[Newtonsoft.Json.JsonConverter]` is ignored too. Use the `System.Text.Json.Serialization` equivalents (`[JsonPropertyName]`, `[JsonConverter]`, `[JsonIgnore]`).
- **A `[Newtonsoft.Json.JsonIgnore]` property is now published.** If you used it to keep data off the wire (a card number, a token, internal notes), switch to `System.Text.Json.Serialization.JsonIgnore` *before* moving to STJ, or that data goes to every subscriber.
- **A property typed as a base class is written as the declared type.** A `Shape Shape` property holding a `Circle` is written with `Shape`'s members only, so `Radius` is dropped. Newtonsoft wrote the runtime type. Use STJ polymorphism (`[JsonDerivedType]`) or a concrete property type. The message itself is unaffected, because JustSaying serializes each message by its runtime type.
- **`object` and `Dictionary<string, object>` values are read as `JsonElement`**, not `long`, `string` or `JObject`, so casts like `(long)extra["retries"]` throw.
- **Reads are strict about types.** STJ throws a `JsonException` (the message goes to redrive and then the error queue) where Newtonsoft converted: quoted numbers (`"Quantity":"5"`), numbers into `string` properties, `"true"` into `bool`, `"NaN"` and `"Infinity"` for `double`, comments and trailing commas. Set `NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals` on a copy of the defaults if you need the numeric cases.
- **Publishing `double.NaN` or infinity throws** (`ArgumentException`), where v8 wrote `"NaN"`.
- **Get-only collections are only populated on types with a parameterless constructor.** On a type bound through its constructor (a positional record, for example), give the collection a constructor parameter or a setter.

#### Keeping Newtonsoft.Json

To keep using Newtonsoft.Json, register the factory yourself. `AddJustSaying` registers the System.Text.Json factory with `TryAddSingleton`, so yours wins if you register it with `AddSingleton` (before or after `AddJustSaying`), or with `TryAddSingleton` before it:

```csharp
using JustSaying.Messaging.MessageSerialization;
using Newtonsoft.Json;

// Exactly v8's wire format: enums as numbers, nulls written.
services.AddSingleton<IMessageBodySerializationFactory>(
    new NewtonsoftSerializationFactory(new JsonSerializerSettings()));

services.AddJustSaying(builder => builder.Messaging(c => c.WithRegion("eu-west-1")));
```

`new NewtonsoftSerializationFactory()` with no settings uses v9's defaults instead (enums as strings, nulls left out). If you passed your own `JsonSerializerSettings` in v8, pass the same ones and the wire format doesn't change:

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

#### Rolling upgrade of a mixed v8/v9 fleet

v8 and v9 services share topics and queues without problems as long as they agree on the body format. To get from v8 to STJ without a flag day:

1. Upgrade every service to v9 with `new NewtonsoftSerializationFactory(new JsonSerializerSettings())`, or the settings you used in v8. The wire stays byte-identical, so v8 and v9 services can deploy in any order.
2. Once no v8 service is left, prepare the message types using the checklist above. Add the System.Text.Json attributes alongside the Newtonsoft ones (each library ignores the other's) and keep both until every service is on STJ. Change SNS filter policies that match enum numbers or `null` in the body to also match the new values.
3. Switch consumers to STJ by removing the Newtonsoft registration, then switch producers. STJ reads what the Newtonsoft settings write (apart from the strict cases in the checklist), so nothing sends the new format until everything can read it.

### Serialization interface is generic

`IMessageBodySerializer` is now the generic `IMessageBodySerializer<T>` on the public surface (an internal type-erased seam handles the runtime boundary). If you implement a custom serializer or serialization factory, update to the generic signatures. Routing and serialization remain by each message's runtime type, as in v8: a single (or batch) publish of a base-typed instance is still routed to, and serialized by, the publisher registered for its concrete type.

### Publications for base classes and interfaces receive derived messages

In v8 a publication for an abstract class or interface (for example `WithTopic<WarehouseEvent>()`) was never used: publishing a derived message failed with "no publishers registered". v9 falls back to it. When no publication is registered for a message's runtime type, the message goes to the publication for its closest base class, otherwise for an interface it implements (if more than one registered interface matches, publishing throws and names them). A publication for the exact runtime type always wins, so existing registrations behave as before. Batches are grouped by the publication each message resolves to. Subscriptions fall back the same way, so a `ForTopic<WarehouseEvent>()` handler receives the derived messages its serializer returns.

The message is serialized as the registered type. With a `[JsonPolymorphic]` base type and the System.Text.Json serializer, the body carries the type discriminator, so a base-type consumer can deserialize it. Without polymorphism configured, `SystemTextJsonMessageBodySerializer<T>` writes only the members of `T`, while `NewtonsoftMessageBodySerializer<T>` writes the runtime type's members but no discriminator.

## Exactly-once handling requires a stable key for non-`Message` payloads

In v8 every message derived from `Message`, so `UseExactlyOnce<TMessage>` always deduplicated on `Message.UniqueKey()`. A message that doesn't derive from `Message` has no such key, so v9 asks you for one:

```csharp
// Message-derived types: unchanged, uses Message.UniqueKey()
pipeline.UseExactlyOnce<OrderAccepted>("orders-handler");

// Non-Message types: provide a stable deduplication key, or registration throws
pipeline.UseExactlyOnce<OrderPlaced>("orders-handler",
    deduplicationKeySelector: m => m.OrderRef);
```

If a non-`Message` type is used without a `deduplicationKeySelector`, `UseExactlyOnce` throws at registration (startup) rather than degrading silently at runtime. A selector that returns null or whitespace for a given message doesn't collapse unrelated messages onto a shared lock key: that message is not handled, an error is logged, and it stays on the queue for its redrive policy.

`UseExactlyOnce<TMessage>` on a subscription whose message type isn't a `TMessage` now throws when the bus is built, rather than failing for every message.

## A subscription's middleware configuration must add the handler

`WithMiddlewareConfiguration` replaces the default pipeline, so it has to add the handler itself with `UseDefaults<T>(handlerType)` or `UseHandler`. In v8 a configuration that didn't (for example one that only called `UseExactlyOnce`) built a pipeline that never ran the handler, and nothing was logged. v9 throws when the bus is built, naming the message type:

```csharp
c.WithMiddlewareConfiguration(m =>
    m.UseExactlyOnce<OrderAccepted>("orders-handler")
     .UseDefaults<OrderAccepted>(typeof(OrderAcceptedHandler)));
```

## New extensibility seams

Available on `IMessagingConfig`:

- **`IMessageTypeRegistry`** — bidirectional map between a message type and its logical wire name (the SNS `Subject` today). `GetLogicalName` preserves existing subject behaviour; `TryResolveType` enables future type-based inbound routing. The native `Subject` remains the unqualified type name.

It has a sensible default and requires no action unless you are customising naming.

A message that doesn't derive from `Message` has no id JustSaying can read, so publish and handle logs show its id as `(null)` and the publish activity has no `messaging.message.id` tag.
