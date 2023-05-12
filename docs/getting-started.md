# Getting Started

**JustSaying** uses conventions based on type names to determine which queues, topics, and subscriptions it should use. On startup, JustSaying ensures that that infrastructure is ready before beginning to publish or consume any messages.

### Events

Events inherit the `JustSaying.Models.Message` class, so that we ensure common properties are available on all messages.

```csharp
public class OrderReadyEvent : Message
{
    public int OrderId { get; set; }
}
```

_Note that events have a maximum serialized size of 256kb_

### Publishing a Message

When JustSaying is added to the built in DI system, it makes an `IMessagePublisher` available that can be used to publish messages.

```csharp
public class OrderAccepter {
    private IMessagePublisher _publisher;

    public OrderAccepter(IMessagePublisher publisher) {
        _publisher = publisher;
    }

    public Task Accept() {
        // do some accepty type things here
        _publisher.PublishAsync(new OrderAccepted());
    }
}
```

### Handlers

Handlers should implement the interface `IHandlerAsync<T>` which JustSaying will call when a message of the specified type is received.

**By default, JustSaying will reserve messages it has downloaded for 30 seconds, and messages will be kept in the queue for 4 days. To override these, see the documentation on** [**`SqsReadConfiguration`**](subscriptions/configuration/sqsreadconfiguration.md)**.**

```csharp
public class OrderReadyEventHandler : IHandlerAsync<OrderReadyEvent>
{
    public async Task<bool> Handle(OrderReadyEvent message)
    {
        // Do something here
        return true;
    }
}
```

#### Return value

Note that the handler returns a `bool`. Handlers should return true if the message was handled successfully, and false if not. If false is returned, then the message won't be deleted from SQS, and will be re-processed by another worker when its visibility timeout expires.

**If an exception is thrown, it is equivalent to the handler returning false.**

If a message is re-processed too many times, it will be sent to the corresponding error queue for the event \(unless the error queue has been disabled\).

### Dependency Injection

### `Microsoft.Extensions.DependencyInjection`

The easiest way to get started with JustSaying is to plug it into the built in .NET Core container. Other containers can be used alongside the built in one, but we recommend using this API as it is actively tested and maintained.

The code sample below configures the following things:

1. A Region to use in AWS. This must be specified as there is no 'default' AWS region.
2. A publisher for an event named `OrderPlacedEvent`
3. A subscriber for an event named `OrderReadyEvent`
4. A handler for the `OrderReadyEvent` called `OrderReadyEventHandler`

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddJustSaying(config =>
        {
            config.Messaging(x =>
            {
                // Configures which AWS Region to operate in
                x.WithRegion(_configuration.GetAWSRegion());
            });

            config.Publications(x =>
            {
                // Creates the following if they do not already exist
                //  - a SNS topic of name `orderplacedevent`
                x.WithTopic<OrderPlacedEvent>();
            });

            config.Subscriptions(x =>
            {
                // Creates the following if they do not already exist
                //  - a SQS queue of name `orderreadyevent`
                //  - a SQS queue of name `orderreadyevent_error`
                //  - a SNS topic of name `orderreadyevent`
                //  - a SNS topic subscription on topic 'orderreadyevent' and queue 'orderreadyevent'
                x.ForTopic<OrderReadyEvent>();
            });
        });

    services.AddJustSayingHandler<OrderReadyEvent, OrderReadyEventHandler>();

}
```

Note that the `AddJustSaying` extension method requires installing the [JustSaying.Extensions.DependencyInjection](https://www.nuget.org/packages/JustSaying.Extensions.DependencyInjection.Microsoft/7.0.0-beta.1) package, which is currently in pre-release.

### Startup

Now that we've created an event and handler, and wired it into the DI container, let's start the bus. How this is done is up to you, but here's an example using the built in `IHostedService` that comes with .NET Core.

_`BackgroundService` is a built-in type that implements `IHostedService` to provide a simple `ExecuteAsync` method with a `CancellationToken`._

```csharp
public class BusService : BackgroundService
{
    private readonly IMessagingBus _bus;
    private readonly ILogger<BusService> _logger;
    private IMessagePublisher _publisher;

    public BusService(IMessagingBus bus, ILogger<BusService> logger, IMessagePublisher publisher)
    {
        _bus = bus;
        _logger = logger;
        _publisher = publisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bus is starting up...");

        await _publisher.StartAsync(stoppingToken);
        await _bus.StartAsync(stoppingToken);
    }
}
```

* Note here that we're calling `StartAsync` on both the `IMessagingBus`, and `IMessagePublisher`. Starting the `IMessagePublisher` ensures that all publish infrastructure is created and ready, while starting the `IMessagingBus` will firstly ensure that all subscription infrastructure is created and ready, and then begin consuming messages.
* The only way to stop the bus is to cancel the cancellation token, which will tear down everything; however you can pause and resume listening on the bus by
using the methods on `IMessageReceivePauseSignal` to control receiving of all messages, even while the bus is running.

