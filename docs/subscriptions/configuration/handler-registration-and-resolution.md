# Handler Registration and Resolution

#### Definition

JustSaying handlers are recognised by their implementing an interface called `IHandlerAsync<T>`, where `T` is the type of the message the handler should handle.

Here, we're creating a handler for OrderAccepted messages. We also need to tell the stack whether we handled the message as expected. We can say things got messy either by returning false, or bubbling up exceptions.

```csharp
public class OrderNotifier : IHandler<OrderAccepted>
{
    public bool Handle(OrderAccepted message)
    {
        // Some logic here ...
        // e.g. bll.NotifyRestaurantAboutOrder(message.OrderId);
        return true;
    }
}
```

#### Registration & Resolution

The default strategy for resolving `IHandlerAsync<T>` handlers is to simply resolve them from the DI container. 

There is a helper method available to register a handler into the container for a message. This method will add a transient \(i.e. a new instance with each resolution\) handler into the container if an equivalent hand

```csharp
services.AddJustSayingHandler<OrderReadyEvent, OrderReadyEventHandler>();
```

It's also perfectly ok to register handlers yourself, as long as they have a service type of `IHandlerAsync<T>` where `T` is the type of the message the handler should handle.

