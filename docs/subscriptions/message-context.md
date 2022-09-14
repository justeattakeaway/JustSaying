# Message Context

When creating a handler for a message, you provide an `IHandler<T>` implementation that will process the JustSaying message. However, sometimes you want to access the raw SQS message itself, i.e. an [Amazon.SQS.Model.Message](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/SQS/TMessage.html) instance. This can be useful for accessing non-domain concepts, such as message attributes like `ApproximateReceiveCount` or the message reciept handle.

JustSaying allows you to get access to this through the type `MessageContext`, which you can access by injecting an `IMessageContextAccessor` into your handler.

`IMessageContextAccessor` internally stores the `MessageContext` for the current process in an `AsyncLocal<MessageContext>`, so the appropriate instance will be available anywhere in the logical callstack while handling a message.

### Example usage:
```csharp
public class OrderReadyEventHandler : IHandlerAsync<OrderReadyEvent>
{
    private readonly IMessageContextAccessor _contextAccessor;
    private readonly ILogger<OrderReadyEventHandler> _logger;

    public OrderReadyEventHandler(IMessageContextAccessor contextAccessor, ILogger<OrderReadyEventHandler> logger)
    {
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    public async Task<bool> Handle(OrderReadyEvent message)
    {
        // This is the AWS SDK Message instance.
        var message = _contextAccessor.Context.Message;

        if (message.Attributes.TryGetValue("ApproximateReceiveCount", out var approximateReceiveCount))
        {
            _logger.LogInformation("ApproximateReceiveCount: {ApproximateReceiveCount}", approximateReceiveCount);
        }

        // Do something here
        return true;
    }
}
```
