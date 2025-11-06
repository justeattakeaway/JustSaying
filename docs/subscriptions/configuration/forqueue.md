# ForQueue

### `ForQueue<T>`

#### Creates a direct subscription to a queue, without a topic. This can be useful for direct 'command' style scenarios where the destination queue is already known at publish time.

* A queue will be created for message of type `T`, using the supplied `IQueueNamingConvention`, applied to the message type `T`. 
  * This convention can be overridden on a case-by-case basis using `WithName` in the queue configuration.
* A dead letter queue will be created, named after the queue name above with an `_error` suffix.

#### Example:

```text
x.ForQueue<OrderReadyEvent>();
```

This describes the following infrastructure:

* An SQS queue of name `orderreadyevent`
* An SQS queue of name `orderreadyevent_error`

Further configuration options can be defined by passing a configuration lambda to the `ForQueue` method.

