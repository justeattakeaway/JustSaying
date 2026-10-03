---
description: >-
  Middleware in JustSaying is an extensibility feature that provides a way to
  add your own code around handlers to add things like custom logging, exactly
  once handling, or de-duplication.
---

# Middleware

### Configuration

#### Defaults

If no custom middleware configuration is provided, each subscription gets a default pipeline. It's the same as calling `UseDefaults<T>(handlerType)`, which adds, from outermost to innermost:

* `MessageContextAccessorMiddleware`, which makes the [message context](/subscriptions/message-context) available
* `LoggingMiddleware`
* `StopwatchMiddleware`, which calls `IMessageMonitor.HandlerExecutionTime`
* `SqsPostProcessorMiddleware`, which deletes a handled message from the queue
* `ErrorHandlerMiddleware`
* `HandlerInvocationMiddleware`, which resolves the handler and calls it

The handler invocation middleware is always the innermost middleware in a pipeline.

#### Adding additional middleware

Middleware pipelines are configured on a per-subscription basis, using `WithMiddlewareConfiguration` on the subscription builder:

```csharp
x.ForTopic<SimpleMessage>(c => c
    .WithMiddlewareConfiguration(m =>
    {
        m.Use<MyMiddleware>();
        m.UseExactlyOnce<SimpleMessage>("simple-message-lock");
        m.UseDefaults<SimpleMessage>(typeof(SimpleMessageHandler));   // Add default middleware pipeline
    }));
```

This will produce the following pipeline, where before/after refer to when code executes before or after the call to the `next` middleware.

`Before - MyMiddleware  
Before - ExactlyOnceMiddleware  
Before - (default middlewares)  
Before - HandlerInvocationMiddleware  
After - HandlerInvocationMiddleware  
After - (default middlewares)  
After - ExactlyOnceMiddleware  
After - MyMiddleware`

A middleware configuration replaces the default pipeline, so it must add the handler, with `UseDefaults<T>(...)` or `UseHandler<T>()`. A configuration that doesn't throws when the bus is built, naming the message type. `UseDefaults<T>(...)` is strongly recommended, because without it handled messages aren't deleted from their queues.

On a [multi-type queue](/subscriptions/configuration/multi-type-queues), pass the middleware configuration to `Handling<T>` for each type instead.

#### Exactly-once handling

`UseExactlyOnce<T>(lockKey)` stops a message being handled more than once, using an `IMessageLockAsync` that you register in the container. For a message type that derives from `Message` it locks on `Message.UniqueKey()`. Any other message type needs a key selector, or `UseExactlyOnce` throws when the bus is built:

```csharp
m.UseExactlyOnce<OrderPlaced>("order-placed-handler", deduplicationKeySelector: o => o.OrderId);
```

`UseExactlyOnce<T>` must be given the subscription's own message type (or a type it derives from). See [Message Types](/messages/#exactly-once-handling) for more.

An example of using custom middleware can be found in the [sample](https://github.com/justeattakeaway/JustSaying/tree/main/samples/src/JustSaying.Sample.Middleware)
