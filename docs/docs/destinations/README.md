---
---

# Destinations

Every publication and subscription targets a topic or a queue. In v9 you describe that target with a value: a `TopicDestination` or a `QueueDestination`. The value says **which** resource to use and, when JustSaying owns it, **how to create it**. The registration's own builder then only configures publish-time or read-time behaviour.

```csharp
config.Publications(x =>
{
    // By convention: the topic is named after the message type ('orderplaced') and created
    x.WithTopic<OrderPlaced>();

    // By name, with tags and encryption for when it's created
    x.WithTopic<OrderShipped>(TopicDestination.Named("order-shipped", t => t
        .WithTag("team", "orders")
        .WithEncryption("alias/orders")));

    // An existing topic, which JustSaying never creates or changes
    x.WithTopic<OrderCancelled>(TopicDestination.FromArn("arn:aws:sns:eu-west-1:111122223333:order-cancelled"));
});
```

## Owned and existing resources

A destination is either **owned** or **existing**:

| | Topic | Queue | On startup |
| --- | --- | --- | --- |
| Owned | `TopicDestination.ByConvention()`, `TopicDestination.Named(name)` | `QueueDestination.ByConvention()`, `QueueDestination.Named(name)` | Created if it doesn't exist, and [updated](/destinations/startup-behaviour) if it does |
| Existing | `TopicDestination.FromArn(arn)` | `QueueDestination.FromArn(arn)`, `QueueDestination.FromUrl(url)`, `QueueDestination.FromUri(uri)` | Used as it is: never created or changed |

Only an owned destination takes infrastructure configuration, so there's no way to set creation settings that would be ignored. `ByConvention` and `Named` take an optional configuration callback:

```csharp
QueueDestination.Named("payments", q => q
    .WithVisibilityTimeout(TimeSpan.FromMinutes(2))
    .WithMessageRetention(TimeSpan.FromDays(7))
    .WithRetriesBeforeErrorQueue(3));

QueueDestination.ByConvention(q => q.WithNoErrorQueue());
```

`FromUrl` and `FromUri` take an optional region name. When it's left out, the region is read from the URL.

## Topic settings

`TopicInfrastructure`, the callback's argument for a topic:

| Method | Description |
| --- | --- |
| `WithTag(key)`, `WithTag(key, value)` | Adds a tag to the topic. |
| `WithEncryption(kmsMasterKeyId)`, `WithEncryption(ServerSideEncryption)` | Encrypts the topic with a KMS key. See [Encryption](/advanced/encryption). |

## Queue settings

`QueueInfrastructure`, the callback's argument for a queue:

| Method | Description | Default |
| --- | --- | --- |
| `WithVisibilityTimeout(timeout)` | How long a received message is hidden from other consumers while it's handled. More than zero and at most 12 hours. | 30 seconds |
| `WithMessageRetention(retention)` | How long a message is kept on the queue, from 1 minute to 14 days. | 4 days |
| `WithDeliveryDelay(delay)` | How long a new message is hidden before it can be received, up to 15 minutes. | 0 |
| `WithRetriesBeforeErrorQueue(count)` | How many times a message is received before it moves to the error queue. Between 1 and 1000. | 5 |
| `WithErrorQueueRetention(retention)` | How long a message is kept on the error queue, from 1 minute to 14 days. | 14 days |
| `WithNoErrorQueue()` | Don't create an `_error` queue and redrive policy. | An error queue is created |
| `WithEncryption(kmsMasterKeyId)`, `WithEncryption(ServerSideEncryption)` | Encrypts the queue, and its error queue, with a KMS key. | Not encrypted |
| `WithTag(key)`, `WithTag(key, value)` | Adds a tag to the queue and its error queue. | |

## Where destinations are used

| Registration | Destination |
| --- | --- |
| [`WithTopic<T>(destination)`](/publishing/withtopic) | `TopicDestination` |
| [`WithQueue<T>(destination)`](/publishing/withqueue) | `QueueDestination` |
| [`ForTopic<T>(destination)`](/subscriptions/configuration/fortopic) | `TopicDestination` (owned only), with the queue set by `WithQueue(QueueDestination)` |
| [`ForQueue<T>(destination)`](/subscriptions/configuration/forqueue) | `QueueDestination` |
| [`ForQueue(destination, ...)`](/subscriptions/configuration/multi-type-queues) | `QueueDestination` (named or existing) |
| [`WithCloudEventTopic<T>`, `WithCloudEventQueue<T>`](/cloudevents/publishing) | `TopicDestination`, `QueueDestination` |

The older shortcuts still work and do the same thing: `WithTopicArn<T>(arn)` is `WithTopic<T>(TopicDestination.FromArn(arn))`, `ForQueueUrl<T>(url)` is `ForQueue<T>(QueueDestination.FromUrl(url))`, and so on. So do `WithTopicName`/`WithQueueName` on the builders, which are the same as `Named(name)` without any infrastructure configuration.

A topic subscription creates its topic if it's missing, but doesn't configure it: give `ForTopic` a destination without infrastructure settings, and configure the topic on the publication.

## Sharing a destination

A destination value is just a value, so declare it once and use it from both sides of a point-to-point queue:

```csharp
var payments = QueueDestination.Named("payments", q => q
    .WithVisibilityTimeout(TimeSpan.FromMinutes(2))
    .WithEncryption("alias/payments"));

config.Publications(x => x.WithQueue<TakePayment>(payments));
config.Subscriptions(x => x.ForQueue<TakePayment>(payments));
```

A publication and a subscription in the same application that declare *different* settings for one queue each apply their own on startup, so whichever starts last wins.

## Validation

Destinations are checked when they're created and when the bus is built, so mistakes fail at startup with the registration named rather than as an AWS error:

* Names may only contain `A-Z`, `a-z`, `0-9`, `-` and `_`. A topic name can be up to 256 characters. A queue name can be up to 80 characters *including* the `_error` suffix of its error queue, so at most 74 unless it has no error queue. A blank name throws rather than falling back to the naming convention.
* FIFO queues and topics (`.fifo` names) aren't supported.
* Queue URLs must have an `{account}/{queue}` path, and a region name that contradicts the URL throws. Queue and topic ARNs must include the region and account.
* A resource can only be named once: naming a destination with `Named(...)` and also calling `WithTopicName` or `WithQueueName` on the builder throws, as does naming an existing (ARN or URL) destination.
* Settings that only make sense for one kind of destination throw for the other. For example, `WithQueueExistenceCheck()` only applies to an existing queue, `WithTopicAddress` only to a topic addressed by ARN, and `WithTopicName(message => ...)` only to an owned topic.

See [Infrastructure on startup](/destinations/startup-behaviour) for what JustSaying does to owned resources each time it starts.
