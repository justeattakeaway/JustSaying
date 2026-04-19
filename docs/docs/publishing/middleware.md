---
---

# Publish Middleware

Publish middleware allows you to add cross-cutting concerns to your publish operations, such as tracing, logging, or metadata enrichment. It follows the same `MiddlewareBase<TContext, TOut>` pattern used for [handler middleware](/subscriptions/middleware/README), but wraps `PublishAsync` and `PublishBatchAsync` calls instead of message handling.

## How It Works

When you call `PublishAsync` or `PublishBatchAsync`, the message passes through the publish middleware pipeline before being sent to SNS/SQS. Each middleware in the pipeline can:

- Inspect or modify the message and metadata before publishing
- Add message attributes to the `PublishMetadata`
- Execute logic after publishing (e.g. record metrics)
- Short-circuit the pipeline by not calling `next`

## PublishContext

Publish middleware receives a `PublishContext` containing:

| Property | Type | Description |
|----------|------|-------------|
| `Message` | `Message` | The message being published (single publish), or `null` for batch |
| `Messages` | `IReadOnlyCollection<Message>` | The messages being published (batch publish), or `null` for single |
| `Metadata` | `PublishMetadata` | The publish metadata — middleware can add message attributes to this |

## Global Middleware

Global publish middleware applies to **all** publish operations as a fallback when no per-publisher middleware is configured. Configure it on the `PublicationsBuilder`:

```csharp
services.AddJustSaying(config =>
{
    config.Publications(x =>
    {
        x.WithTopic<OrderPlacedEvent>();
        x.WithQueue<ProcessPaymentCommand>();

        // Add middleware that applies to all publishers
        x.WithPublishMiddleware<LoggingPublishMiddleware>();
        x.WithPublishMiddleware<MetadataEnrichmentMiddleware>();
    });
});
```

Multiple calls to `WithPublishMiddleware` add middlewares in declaration order (outermost first).

## Per-Publisher Middleware

Per-publisher middleware is configured on individual publication builders using `WithMiddlewareConfiguration`. When configured, it takes **priority over** the global publish middleware for that message type.

```csharp
services.AddJustSaying(config =>
{
    config.Publications(x =>
    {
        // This publisher uses its own middleware pipeline
        x.WithTopic<OrderPlacedEvent>(cfg =>
        {
            cfg.WithMiddlewareConfiguration(m =>
            {
                m.Use<AuditPublishMiddleware>();
                m.Use<LoggingPublishMiddleware>();
            });
        });

        // This publisher falls back to the global middleware
        x.WithQueue<ProcessPaymentCommand>();
    });
});
```

## Writing Custom Publish Middleware

Custom publish middleware derives from `MiddlewareBase<PublishContext, bool>` and implements the `RunInnerAsync` method:

```csharp
public class LoggingPublishMiddleware : MiddlewareBase<PublishContext, bool>
{
    private readonly ILogger<LoggingPublishMiddleware> _logger;

    public LoggingPublishMiddleware(ILogger<LoggingPublishMiddleware> logger)
    {
        _logger = logger;
    }

    protected override async Task<bool> RunInnerAsync(
        PublishContext context,
        Func<CancellationToken, Task<bool>> func,
        CancellationToken stoppingToken)
    {
        var messageType = context.Message?.GetType().Name ?? "batch";

        _logger.LogInformation("Publishing {MessageType}", messageType);

        var result = await func(stoppingToken);

        _logger.LogInformation("Published {MessageType}: {Result}", messageType, result);

        return result;
    }
}
```

**Important:** You must call `func(stoppingToken)` to pass execution to the next middleware in the pipeline. If you don't call it, the message will not be published.

### DI Registration

Publish middlewares must be registered as **transient** in your DI container, as each pipeline resolution requires a new instance:

```csharp
services.AddTransient<LoggingPublishMiddleware>();
```

## PublishMiddlewareBuilder API

The `PublishMiddlewareBuilder` exposes the following methods:

| Method | Description |
|--------|-------------|
| `Use<TMiddleware>()` | Add a middleware resolved from the DI container |
| `Use(middleware)` | Add a specific middleware instance |
| `Use(factory)` | Add a middleware created by a factory function |
| `Configure(action)` | Delegate configuration to an `Action<PublishMiddlewareBuilder>` |

## Example: Metadata Enrichment

A common use case is enriching publish metadata with additional attributes:

```csharp
public class CorrelationIdMiddleware : MiddlewareBase<PublishContext, bool>
{
    protected override async Task<bool> RunInnerAsync(
        PublishContext context,
        Func<CancellationToken, Task<bool>> func,
        CancellationToken stoppingToken)
    {
        // Add a correlation ID to every published message
        context.Metadata.AddMessageAttribute(
            "CorrelationId",
            new MessageAttributeValue
            {
                DataType = "String",
                StringValue = Activity.Current?.Id ?? Guid.NewGuid().ToString()
            });

        return await func(stoppingToken);
    }
}
```
