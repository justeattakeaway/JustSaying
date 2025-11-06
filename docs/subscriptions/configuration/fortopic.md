# ForTopic

### `ForTopic<T>`

#### Creates a topic for which there can be multiple queue subscriptions. This enables a 'fan out' scenario.

* A topic and queue pair will be created for message of type `T`, with a subscription attaching the topic to the queue.
* The topic and queue names will be determined using the supplied \(or default if not\) `ITopicNamingConvention` and `IQueueNamingConvention`, applied to the message type `T`. 
  * These conventions can be overridden on a case-by-case basis using `WithName` in the topic configuration.
* A dead letter queue will be created, named after the queue name above with an `_error` suffix.

#### Example:

```text
x.ForTopic<OrderReadyEvent>();
```

This describes the following infrastructure:

* An SQS queue of name `orderreadyevent` 
* An SQS queue of name `orderreadyevent_error` 
* An SNS topic of name `orderreadyevent` 
* An SNS topic subscription on topic `orderreadyevent` and queue `orderreadyevent`

Further configuration options can be defined by passing a configuration lambda to the `ForTopic` method.

