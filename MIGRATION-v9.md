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
- The `ExactlyOnceMiddleware<T>` constructor has a new `deduplicationKeySelector` parameter (`Func<T, string>`) before `logger`. Prefer `UseExactlyOnce<T>` to constructing it directly.
- Batch publish middleware runs once per message type in the batch. In v8 a batch ran the middleware once, chosen by the type of the first message, with every message in the context. v9 groups a batch by the publication each message resolves to (its runtime type's, or the closest registered base type's) and runs each group through the middleware registered for that publication, with only that group's messages in `PublishContext.Messages`. The groups run concurrently and share the `PublishBatchMetadata` you passed in, so middleware that writes to the metadata for one group affects the others.

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
- **A property typed as a base class is written as the declared type.** A `Shape Shape` property holding a `Circle` is written with `Shape`'s members only, so `Radius` is dropped. Newtonsoft wrote the runtime type. Use STJ polymorphism (`[JsonDerivedType]`) or a concrete property type. The message itself is unaffected when a publication is registered for its own type, because JustSaying then serializes it by its runtime type (for publications of base types, see below).
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

The message is serialized as the registered type. With a `[JsonPolymorphic]` base type and the System.Text.Json serializer, the body carries the type discriminator, so a base-type consumer can deserialize it. Without polymorphism configured, `SystemTextJsonMessageBodySerializer<T>` would write only the members of `T`, so for an abstract class or interface it throws when the bus is built, saying how to configure it. `NewtonsoftMessageBodySerializer<T>` writes the runtime type's members but no discriminator.

### Native AOT setup

JustSaying's own code is trim- and AOT-safe, but the default STJ registration uses reflection, which isn't available under Native AOT. Register a source-generated `JsonSerializerContext` through `SystemTextJsonSerializationFactory`:

```csharp
// Every message type you publish or handle, plus the runtime types of any object-typed values.
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(OrderPlaced))]
[JsonSerializable(typeof(OrderShipped))]
public sealed partial class MessagesJsonContext : JsonSerializerContext;

// Copy JustSaying's defaults so the AOT build reads and writes like the reflection-based default.
var options = new JsonSerializerOptions(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions)
{
    TypeInfoResolver = MessagesJsonContext.Default,
};

services.AddSingleton<IMessageBodySerializationFactory>(new SystemTextJsonSerializationFactory(options));
```

When reflection-based serialization is off, a message type that's missing from the context (or no context at all, as with the default registration) fails when the bus is built, with an `InvalidOperationException` naming the type, rather than at the first publish or receive.

**`UseStringEnumConverter = true` is required** for wire compatibility. The default writes enums as strings using a converter that needs dynamic code, so under Native AOT it isn't there. Without the setting, an AOT service writes enums as numbers and **can't read the strings** that JIT and v8-style producers send: those messages fail to deserialize and end up in the error queue. JustSaying doesn't check this for you.

To reproduce AOT serializer behaviour without publishing natively, set `<PublishAot>true</PublishAot>` in the project. That turns off reflection-based serialization and dynamic code in `dotnet run` and `dotnet test` too. Building with `-p:JsonSerializerIsReflectionEnabledByDefault=false` turns off only reflection: it shows a type missing from the context, but enums still go out as strings because dynamic code is still available.

Newtonsoft.Json isn't supported under Native AOT.

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

The lock key for a **generic** message type now uses its C# spelling (`myapp.envelope<myapp.orderplaced>`) instead of the CLR name, which embedded the type arguments' assembly versions and so changed — silently disabling deduplication — on every deploy. Keys for non-generic types are unchanged.

## One publication per message type

Registering two publications for the same message type (for example `WithTopic<Order>()` twice, or a
`WithTopic<Order>()` alongside a `WithQueue<Order>()`) previously last-write-wins: the earlier
registration was silently discarded. v9 throws at startup instead:

> A publisher for message type 'Order' is already registered. Each message type can only have one publication.

If you hit this, remove the redundant registration — only one of them was ever taking effect.

## Destinations are values: `TopicDestination` and `QueueDestination`

The fluent registration API is rebuilt around two destination values. A `TopicDestination` or `QueueDestination` says
*which* resource a registration targets and — when JustSaying owns it — *how to create it*; the
registration builders configure publish/read-time behaviour only, and are the same type whether
the resource is created by JustSaying or already exists:

```csharp
p.WithTopic<OrderPlaced>();                                   // by convention, created
p.WithTopic<OrderPlaced>(TopicDestination.Named("orders"));              // by name, created
p.WithTopic<OrderPlaced>(TopicDestination.Named("orders", t => t
    .WithTag("team", "payments")
    .WithEncryption(masterKeyId)));                           // creation config lives on the value
p.WithTopic<OrderPlaced>(TopicDestination.FromArn(topicArn));            // pre-existing, never created —
                                                              // no creation config to mis-set

s.ForQueue<Refund>(QueueDestination.Named("refunds", q => q
    .WithMessageRetention(TimeSpan.FromDays(4))
    .WithNoErrorQueue()));
s.ForQueue<Refund>(QueueDestination.FromUri(queueUri));
s.ForQueue(QueueDestination.FromUri(queueUri), q => q                    // multi-type over an existing queue
    .Handling<OrderPlaced>()
    .HandlingCloudEvent<ParcelShipped>("com.example.parcel-shipped"));

s.ForTopic<OrderPlaced>(cfg => cfg
    .WithQueue(QueueDestination.Named("orders-sub", q => q.WithTag("team", "payments")))
    .WithFilterPolicy(filterPolicyJson)
    .WithSubscriptionGroup("orders"));
```

This restructures the v8 fluent surface:

- **Removed:** `WithWriteConfiguration(...)`, `WithReadConfiguration(...)`, `WithTag(...)` on the
  registration builders, the `SnsWriteConfigurationBuilder`/`SqsWriteConfigurationBuilder`/
  `SqsReadConfigurationBuilder` wrappers, and the `TopicAddressPublicationBuilder`/
  `QueueAddressPublicationBuilder`/`QueueAddressSubscriptionBuilder` classes.
- **Where each knob went:** queue/topic creation settings (retention, visibility timeout,
  delivery delay, error-queue settings, encryption, tags) → `TopicDestination.Named`/`QueueDestination.Named`/
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

### What JustSaying does to an owned queue on startup

A `QueueDestination` gives the same queue whether a publication or a subscription creates it: the
same attributes, the same `_error` queue and redrive policy, and the same tags on both queues.
`WithEncryption` now covers the `_error` queue too, which is created with (and updated to) the main
queue's KMS settings; v8 left the error queue unencrypted. The
creation settings are validated when the bus is built, for publications as well as subscriptions
(v8 never validated a publication's queue settings).

v9 also checks names and values that v8 passed through to AWS, so they fail when the destination
value is created or the bus is built, naming the registration, instead of as an AWS error on startup:

- Queue and topic names: only `A-Z a-z 0-9 - _`, at most 256 characters for a topic and 80 for a
  queue *including* the `_error` suffix of its error queue (so at most 74 unless it opts out of an
  error queue). A blank or whitespace name passed to `Named` throws instead of silently falling
  back to the naming convention.
- `.fifo` names throw "FIFO queues are not supported" / "FIFO topics are not supported". JustSaying
  never set a `MessageGroupId`, so publishing to a FIFO queue or topic always failed.
- The visibility timeout must be more than zero and at most 12 hours, and the retries before the
  error queue between 1 and 1000 (unless the queue has no error queue).

On every start, an owned queue that already exists is updated:

- **A subscription** converges the queue to its destination value: every setting it declares, and
  the default for every setting it doesn't (v8 parity).
- **A publication** converges only the settings its destination declares, and leaves the rest as
  they are. A point-to-point queue is usually shared with a subscriber in another service, which owns
  the rest, so `WithQueue<T>()` or `QueueDestination.Named(q)` never resets that subscriber's
  visibility timeout or adds an error queue it opted out of. (v8 only created a publication's queue
  and never updated it.)

The CloudEvents registrations take the same values, so they never need per-address variants:

```csharp
p.WithCloudEventTopic<ParcelShipped>(TopicDestination.FromArn(topicArn),
    "com.example.parcel-shipped", source);
p.WithCloudEventQueue<OrderCancelled>(QueueDestination.FromUrl(queueUrl),
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
    source: new Uri("/parcels", UriKind.Relative));               // CloudEvents

// point-to-point queue publications have a matching registration; the CloudEvents
// serializer is self-describing, so the envelope goes to the queue verbatim
// (no { "Subject", "Message" } wrapper)
p.WithCloudEventQueue<OrderCancelled>("com.example.order-cancelled",
    source: new Uri("/orders", UriKind.Relative));

// topic subscriptions — the topic and queue are named after T, like WithCloudEventTopic<T>
s.ForCloudEventTopic<ParcelShipped>("com.example.parcel-shipped");      // handler receives CloudEvent<T>
s.ForCloudEventTopicData<RefundIssued>("com.example.refund-issued");    // handler receives bare T

// queue subscriptions — one queue can mix native and CloudEvents messages
s.ForQueue("orders", q => q
    .Handling<LegacyOrderPlaced>()                                // native, routed by Subject
    .HandlingCloudEvent<ParcelShipped>("com.example.parcel-shipped")   // handler receives CloudEvent<T>
    .HandlingCloudEventData<OrderCancelled>("com.example.order-cancelled")); // handler receives bare T
```

Use a **relative** `source` (`/parcels`) unless every consumer accepts an absolute URI: AWS.Messaging, for one, rejects an absolute `source`. The value is written exactly as given.

`WithCloudEventTopic<T>`/`WithCloudEventQueue<T>` accept both a bare `T` (the envelope's `id`, `time` and `source` are defaulted) and a `CloudEvent<T>` (to set `source`, `subject`, `dataschema` and extension attributes per message). Their optional `configure` callback is the usual `TopicPublicationBuilder<T>`/`QueuePublicationBuilder<T>`, applied to both shapes (an exception handler or topic-name customizer receives the payload, `CloudEvent<T>.Data`). A minted `id`/`time` is fixed per message instance, so publish retries don't change it.

For an all-CloudEvents application, opt the CloudEvents serializer in as the app-wide default — then plain `WithTopic<T>`/`ForQueue<T>` registrations speak CloudEvents too, and every published type must have a `type` mapped in `CloudEventOptions` (an unmapped type fails at startup):

```csharp
services.AddJustSayingCloudEvents(options =>
{
    options.Source = new Uri("/orders", UriKind.Relative);
    options.MapType<OrderPlaced>("com.example.order-placed");
    options.UseAsDefault = true;
});
```

The `data` payload is serialized with the app's own `IMessageBodySerializationFactory` (whatever `AddJustSaying` uses for its other messages), so a source-generated `JsonSerializerContext` registered once for Native AOT covers CloudEvents data too, and the data's JSON matches the rest of the app. Set `CloudEventOptions.DataSerializationFactory` to use a different one.

Single-type subscriptions can also override their serializer per registration via `WithMessageBodySerializer(IMessageBodySerializer<T>)`, now available on the `ForTopic<T>`/`ForQueue<T>` builders as well as `ForQueueUrl<T>`/`ForQueueArn<T>`.

### What is supported

- **Structured mode only.** The whole event is the message body (`application/cloudevents+json`); binary mode (attributes as SNS/SQS message attributes) is neither written nor read.
- **No batch format.** A JSON array of events (`application/cloudevents-batch+json`) is not supported; publish and consume one event per message.
- **Third-party consumers of a topic** should subscribe with SNS `RawMessageDelivery` enabled, so they receive the CloudEvent itself rather than the SNS notification wrapping it. JustSaying subscribers read either.
- **Data.** JSON `data` is read inline, and JSON sent as `data_base64` is decoded and read the same way. Other media types, an event with both members, and a data-less event (`data` absent or `null`) fail handling — they are retried and then dead-lettered, never handed to a handler with no payload.
- **Validation on read.** An event must have `specversion` `1.0` and non-empty `id`, `source` and `type`; `time`, when present, must be RFC 3339 with an offset. An invalid event fails handling (retried, then dead-lettered) rather than reaching a handler half-populated.
- **Extensions** must be named with lowercase letters and digits only (`tenantid`, not `TenantId` or `tenant-id`); `CloudEvent<T>` rejects anything else. An inbound integer or boolean extension is kept as its JSON text (`"42"`, `"true"`).

### Exactly-once on the CloudEvents id

`CloudEvent<T>` isn't a `Message`, so `UseExactlyOnce` needs a key selector. Key it on the event `id`, which a producer keeps across its own retries:

```csharp
s.ForCloudEventTopic<ParcelShipped>("com.example.parcel-shipped", t => t
    .WithMiddlewareConfiguration(m => m
        .UseExactlyOnce<CloudEvent<ParcelShipped>>("parcel-handler", deduplicationKeySelector: e => e.Id)
        .UseDefaults<CloudEvent<ParcelShipped>>(typeof(ParcelShippedHandler))));
```

### Moving a type from native JustSaying to CloudEvents

A consumer can accept both shapes of a type on one queue while its producer moves from `WithTopic<T>` to `WithCloudEventTopic<T>`:

```csharp
s.ForQueue("parcels", q => q
    .Handling<ParcelShipped>()                                          // the native publications
    .HandlingCloudEventData<ParcelShipped>("com.example.parcel-shipped")); // the CloudEvents ones
```

Both reach the same `IHandlerAsync<ParcelShipped>`. Registration order doesn't matter: on a multi-type queue the discriminators run in a fixed order — any added with `WithDiscriminator`, then the CloudEvents `type`, and the SNS `Subject` last — and the first to recognise a message decides its type. A CloudEvents publication also carries the payload's type name as its SNS `Subject`, but a CloudEvent is always routed by its `type`; one whose `type` isn't registered on the queue is unroutable rather than read as the native type.


## A subscription's middleware configuration must add the handler

`WithMiddlewareConfiguration` replaces the default pipeline, so it has to add the handler itself with `UseDefaults<T>(handlerType)` or `UseHandler`. In v8 a configuration that didn't (for example one that only called `UseExactlyOnce`) built a pipeline that never ran the handler, and nothing was logged. v9 throws when the bus is built, naming the message type:

```csharp
c.WithMiddlewareConfiguration(m =>
    m.UseExactlyOnce<OrderAccepted>("orders-handler")
     .UseDefaults<OrderAccepted>(typeof(OrderAcceptedHandler)));
```

## Publish re-attempts skip failures that can't succeed

`PublishFailureReAttempts` used to retry every publish failure. v9 fails on the first attempt, with the original exception, when retrying can't help:

- serialization failures (`System.Text.Json.JsonException`, `Newtonsoft.Json.JsonException`, `NotSupportedException` and `ArgumentException`, for example a type missing from a source-generated context or a `double.NaN`);
- an `OperationCanceledException` after the caller's cancellation token was cancelled.

AWS and network failures are retried as before. This applies to single and batch publishing.

## Message identity and naming

A message that doesn't derive from `Message` has no id JustSaying can read, so publish and handle logs show its id as `(null)` and the publish activity has no `messaging.message.id` tag. Message naming is unchanged: the SNS `Subject` is still the unqualified type name, customisable through `IMessageSubjectProvider` as in v8.

## Multi-type queues

A single queue can now carry more than one message type. Register every type the queue carries on one subscription, and each inbound message is dispatched to the handler for its own type, resolved from a discriminator on the wire (the SNS `Subject` by default):

```csharp
s.ForQueue("orders", q => q
    .Handling<OrderPlaced>()
    .Handling<OrderCancelled>());
```

A message that no registered type matches (for example a type the producer ships before this consumer handles it, or a mistyped subject) is **not deleted**. It is logged at Error, naming the message id, the queue, what each discriminator found (e.g. `subject 'OrderRefunded'`) and the registered type names, and left on the queue, so the redrive policy moves it to the error queue once its retries are used up. Redrive it from there when a consumer can handle it.

Raw message delivery strips the SNS envelope, and with it the `Subject`. A multi-type subscription that uses raw delivery and routes only by `Subject` (the default) can never route a message, so it now fails when the bus is built; add a discriminator that reads the type from the body or attributes (such as `CloudEventTypeDiscriminator`) or turn raw delivery off.

**Breaking:** a queue can no longer be subscribed to more than once with different types. In v8, `ForQueue<A>()` and `ForQueue<B>()` (or `ForTopic<A>()` and `ForTopic<B>()`) on the same queue name started up fine, but the two subscriptions competed for the queue's messages, so each received some of the other's and deserialized them as the wrong type, with default field values and no error. v9 fails when the bus is built, naming the queue and both types; subscribe once with a multi-type subscription instead, as above. Subscribing one queue to several topics with the *same* message type is still allowed.
