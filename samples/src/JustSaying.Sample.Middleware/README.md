# Middleware Example

The purpose of this application is to demonstrate the usage of middleware in JustSaying.

## Usage and Considerations

By default, all subscriptions will have a standard set of a middleware added which will, among other things;

* Wire up the `IHandlerAsync<T>` for your message
* Emit logs and metrics for message handling
* Deletion of a message if your handler returns successful

When you add your own middleware, you will most likely want to ensure the above behaviour is maintained. This can be achieved as follows;

```csharp
...
x.ForTopic<SampleMessage>((cfg) =>
{
    cfg.WithMiddlewareConfiguration(middlewareBuilder =>
    {
        middlewareBuilder.Use(new FirstMiddleware());
        middlewareBuilder.UseDefaults<SampleMessage>(typeof(SampleMessageHandler)); // Add default middleware pipeline
        middlewareBuilder.Use(new LastMiddleware());
    });
});
```

If you don't call `UseDefaults<T>(...)` then messages won't pass through your handler, nor will they be deleted from their queues so its strongly recommended to use it.
