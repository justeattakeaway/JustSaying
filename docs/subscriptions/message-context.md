# Message Context

When creating a handler for a message, you provide an `IHandler<T>` implementation that will process the JustSaying message. However, sometimes you want to access the raw SQS message itself, i.e. an [Amazon.SQS.Model.Message](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/SQS/TMessage.html) instance. This can be useful for accessing non-domain concepts, such as message attributes like `ApproximateReceiveCount` or the message receipt handle.

JustSaying allows you to get access to this through the type `MessageContext`, which you can access by injecting an `IMessageContextAccessor` into your handler.

`IMessageContextAccessor` internally stores the `MessageContext` for the current message being processed in an [`AsyncLocal<MessageContext>`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.asynclocal-1), so the appropriate instance will be available anywhere in the logical call stack while handling a message.

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

### Attributes

On the `MessageContext` type, there are the following places to get attributes:

**Context.MessageAttributes**

Message attributes are user-defined attributes, and the attributes here are retrieved from the message body.

**Context.Message.MessageAttributes**

`Message` is the AWS SQS SDK type, the `MessageAttributes` here are the ones that present in the `MessageAttributes` XML payload that the message is wrapped in. When using JustSaying, this property will always be empty as we populate `Context.MessageAttributes` from the message body, so don't request to include any message attributes in the response object.

**Context.Message.Attributes**

These are the _"system attributes"_ for the message, in JustSaying we request for the `ApproximateReceiveCount` system attribute to be included, so this should be the only present attribute here, and will always be available.
