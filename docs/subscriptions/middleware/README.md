---
description: >-
  Middleware in JustSaying is an extensibility feature that provides a way to
  add your own code around handlers to add things like custom logging, exactly
  once handling, or de-duplication.
---

# Middleware

### Configuration

#### Defaults

There are two middlewares that are added to the message handling pipeline by default: `HandlerInvocationMiddleware` and `StopwatchMiddleware`.

The handler invocation middleware is always the innermost middleware in a pipeline, and is responsible for resolving a handler and calling it.

The stopwatch middleware wraps the handler invocation one, and ensures that the `IMessageMonitor.HandlerExecutionTime`  method is called correctly.

If no custom middleware configuration is provided, the above handlers will be added.

#### Adding additional middleware

Middleware pipelines are configured on a per-subscription basis, using the [`SqsReadConfiguration.WithMiddlewareConfiguration`](../configuration/sqsreadconfiguration.md#withmiddlewareconfiguration) API:

```csharp
c.WithReadConfiguration(rc =>
    rc.WithMiddlewareConfiguration(m =>
    {
        m.Use<MyMiddleware>();
        m.UseExactlyOnce<SimpleMessage>("simple-message-lock");
        m.UseDefaults<SimpleMessage>(typeof(SimpleMessageHandler));   // Add default middleware pipeline
    }))
```

This will produce the following pipeline, where before/after refer to when code executes before or after the call to the `next` middleware.

`Before - MyMiddleware  
Before - ExactlyOnceMiddleware  
Before - StopwatchMiddleware  
Before - HandlerInvocationMiddleware  
After - HandlerInvocationMiddleware  
After - StopwatchMiddleware  
After - ExactlyOnceMiddleware  
After - MyMiddleware`

If you don't call `UseDefaults<T>(...)`` then messages won't pass through your handler, nor will they be deleted from their queues, so it's strongly recommended to use it.

An example of using custom middleware can be found in the [sample](https://github.com/justeattakeaway/JustSaying/tree/main/samples/src/JustSaying.Sample.Middleware)




