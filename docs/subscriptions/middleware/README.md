---
description: >-
  Middleware in JustSaying is an extensibility feature that provides a way to
  add your own code around handlers to add things like custom logging, exactly
  once handling, or de-duplication.
---

# Middleware

### Configuration

#### Defaults

There are two middlewares that are always added to the message handling pipeline: `HandlerInvocationMiddleware` and `StopwatchMiddleware`. 

The handler invocation middleware is always the innermost middleware in a pipeline, and is responsible for resolving a handler and calling it.

The stopwatch middleware wraps the handler invocation one, and ensures that the `IMessageMonitor.HandlerExecutionTime`  method is called correctly.

These defaults cannot currently be removed, but you can add additional middleware around them.

#### Adding additional middleware

Middleware pipelines are configured on a per-subscription basis, using the [`SqsReadConfiguration.WithMiddlewareConfiguration`](../configuration/sqsreadconfiguration.md#withmiddlewareconfiguration) API:

```text
c.WithReadConfiguration(rc =>
    rc.WithMiddlewareConfiguration(m =>
    {
        m.Use<MyMiddleware>();
        m.UseExactlyOnce<SimpleMessage>("simple-message-lock");
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





