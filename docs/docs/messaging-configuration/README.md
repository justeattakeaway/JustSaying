---
---

# Messaging Configuration

Some settings aren't specific to either subscriptions or publications. They can be configured using the `MessagingConfigurationBuilder`.

```csharp
services.AddJustSaying((MessagingBusBuilder config) =>
{
    config.Messaging((MessagingConfigurationBuilder messagingConfig) =>
    {
        // here
    });

});
```

#### `WithRegion`

Specifies a string or RegionEndpoint to connect to

#### `WithPublishFailureReattempts`

Specifies the number of times a publication failure should be retried before throwing an exception.

#### `WithPublishFailureBackoff`

Specifies the amount of time to additionally wait after each publish failure attempt. For example, if this is set to 100ms and PublishFailureReattempts is set to 3, then the wait durations between each failure would be: \[0ms, 100ms, 200ms\].

* There is no wait duration before the first publish attempt.
* This is a linear backoff

#### `WithPublishFailureReattemptsForBatch`

Specifies the number of times a batch publication failure should be retried before throwing an exception. This is separate from single message retry configuration.

```csharp
config.Messaging(x =>
{
    x.WithPublishFailureReattemptsForBatch(3); // Retry batch publishing 3 times
});
```

#### `WithPublishFailureBackoffForBatch`

Specifies the amount of time to additionally wait after each batch publish failure attempt. Uses linear backoff like single message publishing.

```csharp
config.Messaging(x =>
{
    x.WithPublishFailureBackoffForBatch(TimeSpan.FromSeconds(2));
});
```

#### `WithMessageSubjectProvider`

Provides an extensibility point that allows you to customise how the `Subject` is set for messages sent to SNS. 

Note that if this is changed, any other consumer of messages published by this app will need to use the same provider, or risk losing messages.

#### `WithQueueNamingConvention`/`WithTopicNamingConvention`

Provides a way to customise the way queue and topic names are generated from type names. For more information see the [documentation on naming conventions](/messaging-configuration/naming-conventions).

#### `WithMessageResponseLogger`

Provides a custom logger for message publish responses. Useful for debugging or tracking message publish operations.

```csharp
config.Messaging(x =>
{
    x.WithMessageResponseLogger((response, message) =>
    {
        Console.WriteLine($"Published message {message.Id}: {response.MessageId}");
    });
});
```

For batch publishing:

```csharp
config.Messaging(x =>
{
    x.WithMessageResponseLogger((batchResponse, messages) =>
    {
        Console.WriteLine($"Published {messages.Count} messages");
    });
});
```

#### `WithAdditionalSubscriberAccounts`

Specifies additional AWS account IDs that are allowed to subscribe to topics created by this application. Used for cross-account messaging scenarios.

```csharp
config.Messaging(x =>
{
    x.WithAdditionalSubscriberAccounts("123456789012", "234567890123");
});
```

