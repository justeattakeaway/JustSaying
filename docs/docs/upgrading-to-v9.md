---
---

# Upgrading to v9

JustSaying v9 lets any class be a message, splits topic and queue configuration out into [destinations](/destinations/), switches the default serializer to System.Text.Json, and adds [multi-type queues](/subscriptions/configuration/multi-type-queues), [CloudEvents](/cloudevents/), [AsyncAPI](/asyncapi/) and [Native AOT](/advanced/native-aot) support.

The full list of breaking changes is in the [migration guide](https://github.com/justeattakeaway/JustSaying/blob/main/MIGRATION-v9.md). These are the things to check first.

### 1. Batch publishing is `PublishBatchAsync`

`publisher.PublishAsync(messages)` with a collection **still compiles, then throws at runtime**: it now binds to the single-message `PublishAsync<TMessage>`, and there's no publication for `List<T>`. The compiler won't find these call sites, so search for them and rename them to `PublishBatchAsync`. See [Batch Publishing](/publishing/batch-publishing).

### 2. The default serializer is System.Text.Json

The defaults are close to Newtonsoft.Json, but the wire format changes: enums are written as strings, `null` properties are left out, and whole-number `double`s are written as `1` rather than `1.0`. Before you deploy, check your message types:

* **Newtonsoft.Json attributes are ignored.** A `[Newtonsoft.Json.JsonIgnore]` property **is published**, so if you used it to keep data such as a card number off the wire, switch to `System.Text.Json.Serialization.JsonIgnore` first. `[JsonProperty("name")]` names are lost in both directions.
* **SNS filter policies** that match an enum's number, or `null`, in the message body stop matching.
* **Reads are stricter.** Quoted numbers, numbers in `string` properties and `"NaN"` throw, and the message goes to the error queue.
* **A property typed as a base class** is written with only the base class's members.
* **`object` and `Dictionary<string, object>` values** are read as `JsonElement`.

To keep v8's wire format exactly, register `new NewtonsoftSerializationFactory(new JsonSerializerSettings())`. See [Serialization](/messages/serialization).

### 3. Upgrade a mixed fleet in three steps

v8 and v9 services interoperate as long as they agree on the body format:

1. Upgrade every service to v9 with `new NewtonsoftSerializationFactory(new JsonSerializerSettings())` (or the settings you used in v8). The wire format doesn't change, so services can deploy in any order.
2. When no v8 service is left, fix the message types using the list above. Add the System.Text.Json attributes alongside the Newtonsoft ones, and update filter policies to match the new values as well as the old.
3. Switch consumers to System.Text.Json by removing the Newtonsoft registration, then switch producers.

### 4. `WithReadConfiguration`, `WithWriteConfiguration` and `WithTag` are gone

Settings for creating a topic or queue (retention, visibility timeout, error queue, encryption, tags) move to a `TopicDestination` or `QueueDestination`. Settings for publishing and reading (subject, compression, subscription group, filter policy, raw delivery) move to the registration builder. See [Publication Settings](/publishing/write-configuration) and [Subscription Settings](/subscriptions/configuration/sqsreadconfiguration).

:::warning
**Port every `WithReadConfiguration` setting; don't delete the call.** A subscription resets its queue to the defaults for anything its destination doesn't declare, on every start. Deleting a v8 `WithReadConfiguration(...)` resets the live queue on the next deploy: the visibility timeout goes back to 30 seconds, retention to 4 days and retries to 5, and a queue that opted out of an error queue gets one.
:::

### 5. Publication queues are now kept up to date

A publication's queue is updated on every start with the settings its destination declares (v8 only ever created it), and an `_error` queue now gets the same encryption as its queue. Encryption and tags are never removed. See [Infrastructure on Startup](/destinations/startup-behaviour).

### 6. Mistakes that were silent now throw at startup

* Two publications for the same message type.
* Two subscriptions for different message types on the same queue, or the same subscription registered twice. Use a [multi-type queue](/subscriptions/configuration/multi-type-queues#one-subscription-per-queue) instead, which can subscribe to topics too.
* A `WithMiddlewareConfiguration` that doesn't add the handler with `UseDefaults<T>` or `UseHandler<T>`.
* Invalid topic and queue names, including names too long for their `_error` queue and `.fifo` names, and out-of-range settings.

### 7. Extension points take `object`

Messages no longer have to derive from `Message`, so the framework's own callbacks can't promise one. `IMessageMonitor.Handled`, `IMessageBackoffStrategy.GetBackoffDuration`, the message response loggers, `HandleMessageContext.Message` and `PublishContext.Message` are now typed as `object`. Pattern match where you used `Message` members, or use `context.MessageAs<T>()` in handler middleware. Hand-written `IMessagePublisher` fakes need to implement the new generic `PublishAsync<TMessage>` methods.

### 8. Exactly-once handling needs a key for plain messages

`UseExactlyOnce<T>` throws for a type that doesn't derive from `Message` unless you give it a `deduplicationKeySelector`. The lock key for a generic message type has also changed. See [Message Types](/messages/#exactly-once-handling).

### 9. Publish retries skip failures that can't succeed

Serialization failures, and cancellation of the caller's token, fail on the first attempt instead of being retried.
