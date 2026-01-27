---
---

# WithTopic

### `WithTopic<T>()`

Creates an SNS topic for publishing messages of type `T`. Multiple subscribers can receive messages published to this topic, enabling fan-out scenarios where different services react to the same event.

* An SNS topic will be created for messages of type `T`.
* The topic name will be determined using the supplied \(or default if not\) `ITopicNamingConvention`, applied to the message type `T`.
  * This convention can be overridden using `WithTopicName` in the topic configuration.

#### Example:

```csharp
config.Publications(x =>
{
    x.WithTopic<OrderPlacedEvent>();
});
```

This describes the following infrastructure:

* An SNS topic of name `orderplacedevent`

Further configuration options can be defined by passing a configuration lambda to the `WithTopic` method.

#### Example with configuration:

```csharp
config.Publications(x =>
{
    x.WithTopic<OrderReadyEvent>(cfg =>
    {
        cfg.WithTag("IsOrderEvent")
           .WithTag("Publisher", "KitchenConsole");
    });
});
```

### `WithTopicArn<T>(string topicArn)`

Publishes messages of type `T` to an existing SNS topic specified by its ARN \(Amazon Resource Name\). Use this when the topic already exists and you don't want JustSaying to create or manage it.

#### Example:

```csharp
config.Publications(x =>
{
    x.WithTopicArn<OrderPlacedEvent>("arn:aws:sns:us-east-1:123456789012:existing-topic");
});
```

This configuration allows publishing to a topic that may be owned by another AWS account or managed separately from your JustSaying application.

### Configuration Options

When using the configuration lambda, the following options are available:

#### `WithTopicName(string name)`

Override the naming convention to use a specific topic name.

```csharp
x.WithTopic<OrderPlacedEvent>(cfg =>
{
    cfg.WithTopicName("custom-order-topic");
});
```

#### `WithTopicName(Func<Message, string> topicNameFunc)`

Dynamically determine the topic name based on the message content. Useful for multi-tenant scenarios.

```csharp
x.WithTopic<OrderPlacedEvent>(cfg =>
{
    cfg.WithTopicName(msg =>
    {
        var order = (OrderPlacedEvent)msg;
        return $"tenant-{order.TenantId}-orders";
    });
});
```

See [Dynamic Topics](../advanced/dynamic-topics.md) for more details.

#### `WithTag(string key)`

Add a tag to the SNS topic resource \(without a value\).

#### `WithTag(string key, string value)`

Add a tag with a key and value to the SNS topic resource. Tags are useful for cost allocation and resource management in AWS.

```csharp
x.WithTopic<OrderPlacedEvent>(cfg =>
{
    cfg.WithTag("Environment", "Production")
       .WithTag("Team", "Orders");
});
```

#### `WithWriteConfiguration(Action<SnsWriteConfigurationBuilder> configure)`

Configure advanced publishing options such as encryption, compression, and error handling. See [Write Configuration](write-configuration.md) for details.

```csharp
x.WithTopic<OrderPlacedEvent>(cfg =>
{
    cfg.WithWriteConfiguration(w =>
    {
        w.Encryption = new ServerSideEncryption
        {
            KmsMasterKeyId = "your-kms-key-id"
        };
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
