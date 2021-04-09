# Configuration

Configuring subscription groups consists of firstly defining a group and its configuration, and then assigning queue and topic subscriptions to it. 

**Define a group called `orders`**

```csharp
services.AddJustSaying(config =>
{
    config.Subscriptions(subscriptionConfig =>
    {
        subscriptionConfig.WithSubscriptionGroup("orders",
            groupConfig =>
            {
                // configure here!
            });
...
```

**Assign queue and topic subscriptions to `orders`**

```csharp
...
    config.ForTopic<OrderReadyEvent>(topicConfig =>
    {
        topicConfig.WithReadConfiguration(readConfig =>
        {
            readConfig.WithSubscriptionGroup("orders");
        });
    });
...
```

## Options

Like with`SqsReadConfiguration`, there are `WithXxx` methods available on `groupConfig` to configure this subscription group.

#### `WithPrefetch`

Specifies the number of messages that JustSaying should attempt to read from SQS each time it makes a call. For high volume, low duration handlers, this should be left at the default maximum of 10. For longer, slower workloads, reduce this to avoid downloading more work than can be handled by a single node.

#### `WithBufferSize`

While the prefetch determines how much is downloaded from SQS with each attempt, this parameter specifies how many messages should be buffered into the `IMessageReceiveBuffer` before no more attempts to download messages are made. This is a per-queue buffer. Defaults to 10.

#### `WithMultiplexerCapacity`

Specifies the size of the buffer shared across all queues in this subscription group. Defaults to 100.

#### `WithConcurrencyLimit`

Specifies the maximum number of workers that may process messages concurrently from this subscription group. Defaults to `4 * Environment.ProcessorCount`.

#### `WithReceiveMessageWaitTime` 

Specifies the long polling duration that the `IMessageReceiveBuffer` will wait for messages to become available from SQS before trying again. Defaults to 20 seconds. Setting this to 0 will disable long polling from SQS, as per [the docs](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-short-and-long-polling.html#sqs-long-polling).

#### `WithReceiveBufferReadTimeout`

Specifies how long the `IMessageReceiveBuffer` should wait to get a response from SQS when downloading messages. If this duration is reached without a response, this operation will be cancelled and a new one started. The default is 5 minutes.

