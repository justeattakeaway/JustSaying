# JustSaying.Extensions.HeaderPropagation

Automatically propagates HTTP request headers (e.g. `x-correlation-id`, `x-feature-branch`) from incoming ASP.NET Core requests into outgoing SNS/SQS messages as message attributes, and provides per-subscription filter middleware for feature-branch routing on the consume side.

## Installation

```shell
dotnet add package JustSaying.Extensions.HeaderPropagation
```

## Getting started

### 1. Register the extension

Call `AddJustSayingHeaderPropagation` **after** `AddJustSaying` and pass the header names you want propagated. Also register `IHttpContextAccessor` (required to read the current HTTP request).

```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();

builder.Services.AddJustSaying(bus =>
{
    // ... configure topics, subscriptions, etc.
});

builder.Services.AddJustSayingHeaderPropagation("x-correlation-id", "x-feature-branch");
```

Every call to `IMessagePublisher.PublishAsync` will now copy the configured headers from the current HTTP request into the outgoing message's SNS/SQS attributes. If no HTTP context is active (e.g. background workers), the call is a no-op and the message is published without those attributes.

### 2. Read propagated headers in a handler

Inject `IMessageContextReader` to access message attributes inside a handler:

```csharp
public class OrderPlacedHandler : IHandlerAsync<OrderPlaced>
{
    private readonly IMessageContextReader _contextReader;

    public OrderPlacedHandler(IMessageContextReader contextReader)
        => _contextReader = contextReader;

    public async Task<bool> Handle(OrderPlaced message)
    {
        var correlationId = _contextReader.MessageContext?
            .MessageAttributes.Get("x-correlation-id")?.StringValue;

        // use correlationId for logging, tracing, etc.
        return true;
    }
}
```

### 3. Feature-branch routing (optional)

Use `UseHeaderFilter` on a subscription's middleware pipeline so that each deployed instance only processes messages intended for it:

```csharp
builder.Services.AddJustSaying(bus =>
{
    bus.Subscriptions(sub =>
    {
        sub.ForTopic<OrderPlaced>(topic =>
            topic.WithMiddlewareConfiguration(m =>
                m.UseHeaderFilter(
                    "x-feature-branch",
                    Environment.GetEnvironmentVariable("BRANCH_NAME"))));
    });
});
```

**Routing semantics**

| `UseHeaderFilter` configuration | Message has `x-feature-branch: my-branch` | Message has no `x-feature-branch` |
|---|---|---|
| `UseHeaderFilter("x-feature-branch", "my-branch")` | Processed | Skipped |
| `UseHeaderFilter("x-feature-branch", null)` | Skipped | Processed |
| No filter | Processed | Processed |

- Pass the branch name as `expectedValue` to route only messages that carry that branch tag.
- Pass `null` (the default) to route only messages that have no branch tag — this is the production/default lane.
- Skipped messages are acknowledged (not re-queued).
- Comparison is ordinal and case-sensitive.

## Notes

- `AddJustSayingHeaderPropagation` must be called **after** `AddJustSaying`. If called before, no publisher is found and the publish-side wrapping is silently skipped.
- `IHttpContextAccessor` must be registered before the `IMessagePublisher` singleton is first resolved. Calling `services.AddHttpContextAccessor()` in `Program.cs` is sufficient.
- Duplicate calls with the same header key overwrite the previous value (last write wins), consistent with how the core library handles message attributes.
