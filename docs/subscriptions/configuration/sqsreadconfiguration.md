# SqsReadConfiguration

There are many toggles and settings available to tune JustSaying to your workload. For subscriptions, these are accessible via the `WithReadConfiguration` method when using the fluent API:

```csharp
x.ForTopic<OrderReadyEvent>(c =>
{
    c.WithReadConfiguration(rc =>
    {
        // configure here!
    });
});
```

Note that these configuration options are per-\(queue/topic\).

#### `WithEncryption`

Configures this subscription to use message level encryption, with keys stored in AWS Key Management Service. Provide the KMS Key Id, and optionally a caching period for the key, which defaults to 5 minutes.

#### `WithErrorQueue`

Specifies that an error queue should be created for this subscription. This is the default behaviour.

#### `WithNoErrorQueue`

Specifies that no error queue should be created for this subscription. This will also disable the error queue behaviour for this subscription.

#### `WithErrorQueueOptOut`

Specifies an explicit opt in/out for an error queue. `WithErrorQueue`/`WithNoErrorQueue` delegate to this.

#### `WithMessageRetention`

Specifies the duration for which messages should be kept in this queue before being automatically deleted. The default is 4 days.

#### `WithVisibilityTimeout`

Specifies for how long a message should be invisible to other consumers while it is being handled. The default is 30 seconds. For messages that take a long time to handle, this should be increased to avoid duplicate handling.

#### `WithTopicSourceAccount`

Specifies that the topic for this subscription belongs to a different AWS account than the current one. When this is set, a cross-account topic subscription will be created so that messages delivered to the specified account's topic will be delivered to a queue in this account.

#### `WithSubscriptionGroup`

Specifies that this subscription belongs to a [subscription group](../subscriptiongroups/). By default, each queue or topic subscription gets its own group.

#### `WithMiddlewareConfiguration`

Provides a way to customise the middleware pipeline for this subscription. For more information, see the documentation on [middleware](../middleware/).



