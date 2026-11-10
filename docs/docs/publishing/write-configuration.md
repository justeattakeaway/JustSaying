---
title: Publication Settings
---

# Publication Settings

Before v9, publications were configured with `WithWriteConfiguration`, which mixed settings for creating the topic or queue with settings for publishing to it. In v9 they're split:

* Settings for **creating** the topic or queue (encryption, tags, retention, visibility timeout, error queue) go on its [destination](/destinations/).
* Settings for **publishing** go on the publication builder, the optional callback of `WithTopic<T>` and `WithQueue<T>`.

```csharp
config.Publications(x =>
{
    x.WithTopic<OrderPlacedEvent>(
        TopicDestination.Named("orders", t => t.WithEncryption("alias/orders")),
        cfg => cfg
            .WithSubject("OrderPlaced")
            .WithCompression(new PublishCompressionOptions
            {
                CompressionEncoding = ContentEncodings.GzipBase64,
                MessageLengthThreshold = 100_000,
            }));

    x.WithQueue<TemporaryCommand>(
        QueueDestination.ByConvention(q => q
            .WithMessageRetention(TimeSpan.FromHours(1))
            .WithNoErrorQueue()
            .WithVisibilityTimeout(TimeSpan.FromMinutes(10))),
        cfg => cfg.WithRawMessages());
});
```

## Publish-time settings

| Setting | Topic (`TopicPublicationBuilder<T>`) | Queue (`QueuePublicationBuilder<T>`) |
| --- | --- | --- |
| Subject | `WithSubject(subject)` | `WithSubject(subject)` |
| Compression | `WithCompression(options)`, see [Compression](../advanced/compression.md) | `WithCompression(options)` |
| Publish exception handler | `WithExceptionHandler(handler)` | |
| Send the body without JustSaying's wrapper | | `WithRawMessages()` |
| Per-message destination | `WithTopicName(message => ...)`, `WithTopicAddress((arn, message) => ...)`, see [Dynamic Topics](../advanced/dynamic-topics.md) | |
| Middleware | `WithMiddlewareConfiguration(...)`, see [Publish Middleware](middleware.md) | `WithMiddlewareConfiguration(...)` |
| Check an existing queue on startup | | `WithQueueExistenceCheck()` |

## Porting `WithWriteConfiguration`

| v8 | v9 |
| --- | --- |
| `w.Encryption = ...`, `w.WithEncryption(...)` | `WithEncryption(...)` on the `TopicDestination` or `QueueDestination` |
| `cfg.WithTag(...)` | `WithTag(...)` on the destination |
| `w.WithMessageRetention(...)`, `w.WithVisibilityTimeout(...)`, `w.WithNoErrorQueue()` | The same methods on the `QueueDestination` |
| `w.WithErrorQueue()`, `w.WithErrorQueueOptOut(false)` | Nothing: an error queue is the default |
| `w.WithQueueName(name)` | `QueueDestination.Named(name)` or `cfg.WithQueueName(name)` |
| `w.Subject = ...` | `cfg.WithSubject(...)` |
| `w.CompressionOptions = ...` | `cfg.WithCompression(...)` |
| `w.HandleException = ...` | `cfg.WithExceptionHandler(...)`, now typed on the message: `Func<Exception, T, bool>` |
| `w.IsRawMessage = true` on a queue | `cfg.WithRawMessages()` |
| `w.IsRawMessage = true` on a topic | Removed. It never changed what was published to a topic: raw delivery is a subscription setting, `WithRawMessageDelivery()`. |

The full mapping, including subscriptions, is in the [migration guide](https://github.com/justeattakeaway/JustSaying/blob/main/MIGRATION-v9.md#porting-v8-configuration-setting-by-setting).

## Advanced Topics

For more information on specific configuration topics, see:

- [Compression](../advanced/compression.md) - Reducing message size and costs
- [Encryption](../advanced/encryption.md) - Securing messages with AWS KMS
- [Dynamic Topics](../advanced/dynamic-topics.md) - Multi-tenant message routing
