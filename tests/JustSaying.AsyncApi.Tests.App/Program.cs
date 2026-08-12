using JustSaying;
using JustSaying.AsyncApi.Tests.App;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("eu-west-1"));
    config.Publications(x => x.WithTopic<OrderReadyEvent>());
    config.Subscriptions(x => x.ForTopic<OrderPlacedEvent>());
});

if (Environment.GetEnvironmentVariable("JUSTSAYING_TESTAPP_SKIP_HANDLER") != "1")
{
    builder.Services.AddJustSayingHandler<OrderPlacedEvent, OrderPlacedEventHandler>();
}

if (Environment.GetEnvironmentVariable("JUSTSAYING_TESTAPP_SKIP_ASYNCAPI") != "1")
{
    builder.Services.AddJustSayingAsyncApi(options => options.Title = "Test App");
}

var host = builder.Build();

// The document generation tool aborts the host inside Build(), so nothing below runs during
// build-time generation. Running the app directly just exits without starting the bus.
_ = host;

namespace JustSaying.AsyncApi.Tests.App
{
    public class OrderPlacedEvent : Message
    {
        public int OrderId { get; set; }
    }

    public class OrderReadyEvent : Message
    {
        public int OrderId { get; set; }
    }

    public class OrderPlacedEventHandler : IHandlerAsync<OrderPlacedEvent>
    {
        public Task<bool> Handle(OrderPlacedEvent message) => Task.FromResult(true);
    }
}
