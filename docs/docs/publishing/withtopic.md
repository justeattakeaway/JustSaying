---
---

# WithTopic

### `WithTopic<T>()`

Creates an SNS topic for publishing messages of type `T`. Multiple subscribers can receive messages published to this topic, enabling fan-out scenarios where different services react to the same event.

* An SNS topic will be created for messages of type `T`.
* The topic name will be determined using the supplied \(or default if not\) `ITopicNamingConvention`, applied to the message type `T`.
  * This convention can be overridden by passing a [destination](/destinations/) such as `TopicDestination.Named("...")`, or with `WithTopicName` in the topic configuration.

#### Example:

```csharp
config.Publications(x =>
{
    x.WithTopic<OrderPlacedEvent>();
});
```

This describes the following infrastructure:

* An SNS topic of name `orderplacedevent`

### `WithTopic<T>(TopicDestination destination)`

Publishes messages of type `T` to the topic described by a [`TopicDestination`](/destinations/). Use it to name the topic, or to configure how it's created:

```csharp
config.Publications(x =>
{
    x.WithTopic<OrderReadyEvent>(TopicDestination.Named("order-ready", t => t
        .WithTag("IsOrderEvent")
        .WithTag("Publisher", "KitchenConsole")
        .WithEncryption("alias/orders")));
});
```

Tags and encryption belong to the topic, so they're set on the destination. They're applied when JustSaying creates the topic, and added to it on later starts. See [Infrastructure on Startup](/destinations/startup-behaviour).

### `WithTopicArn<T>(string topicArn)`

Publishes messages of type `T` to an existing SNS topic specified by its ARN \(Amazon Resource Name\). Use this when the topic already exists and you don't want JustSaying to create or manage it. It's the same as `WithTopic<T>(TopicDestination.FromArn(topicArn))`.

#### Example:

```csharp
config.Publications(x =>
{
    x.WithTopicArn<OrderPlacedEvent>("arn:aws:sns:us-east-1:123456789012:existing-topic");
});
```

This configuration allows publishing to a topic that may be owned by another AWS account or managed separately from your JustSaying application.

### Configuration Options

Every overload takes an optional `Action<TopicPublicationBuilder<T>>` that configures how messages are published:

```csharp
x.WithTopic<OrderPlacedEvent>(cfg => cfg.WithSubject("OrderPlaced"));
x.WithTopic<OrderPlacedEvent>(TopicDestination.Named("orders"), cfg => cfg.WithSubject("OrderPlaced"));
```

#### `WithTopicName(string name)`

Override the naming convention to use a specific topic name. This is the same as using `TopicDestination.Named(name)`, so don't do both.

```csharp
x.WithTopic<OrderPlacedEvent>(cfg =>
{
    cfg.WithTopicName("custom-order-topic");
});
```

#### `WithTopicName(Func<T, string> topicNameCustomizer)`

Determine the topic name from each message as it's published. Useful for multi-tenant scenarios. The topic is created the first time a message is published to it.

```csharp
x.WithTopic<OrderPlacedEvent>(cfg =>
{
    cfg.WithTopicName(order => $"tenant-{order.TenantId}-orders");
});
```

See [Dynamic Topics](../advanced/dynamic-topics.md) for more details. For a topic addressed by ARN, use `WithTopicAddress((arn, message) => ...)` instead.

#### `WithSubject(string subject)`

Set the SNS `Subject` of published messages, instead of the message type's name. Subscribers use the subject to identify the message type, so change it with care.

#### `WithCompression(PublishCompressionOptions options)`

Compress large message bodies. See [Compression](../advanced/compression.md).

#### `WithExceptionHandler(Func<Exception, T, bool> handler)`

Handle a publish failure yourself. Return `true` to mark the exception as handled, or `false` to rethrow it. A second overload, `Func<Exception, IReadOnlyCollection<T>, bool>`, handles batch publish failures.

```csharp
x.WithTopic<OrderPlacedEvent>(cfg =>
{
    cfg.WithExceptionHandler((Exception exception, OrderPlacedEvent message) =>
    {
        logger.LogError(exception, "Failed to publish order {OrderId}", message.OrderId);
        return false;
    });
});
```

The lambda's parameter types are needed because of the batch overload.

#### `WithMiddlewareConfiguration(Action<PublishMiddlewareBuilder> middlewareConfiguration)`

Configure a per-publisher middleware pipeline for this topic. When set, this takes priority over the global publish middleware. See [Publish Middleware](middleware.md) for details.

```csharp
x.WithTopic<OrderPlacedEvent>(cfg =>
{
    cfg.WithMiddlewareConfiguration(m =>
    {
        m.Use<AuditPublishMiddleware>();
        m.Use<LoggingPublishMiddleware>();
    });
});
```

## When to Use Topics

Use topics when:
- Multiple services need to react to the same event \(fan-out pattern\)
- You're publishing events that represent something that has happened
- You need to decouple publishers from subscribers
- Subscribers can be added or removed without changing the publisher

For point-to-point messaging where only one consumer should process the message, use [WithQueue](withqueue.md) instead.
