using JustSaying.Messaging;
using JustSaying.Messaging.Middleware;
using JustSaying.Sample.Restaurant.Models;
using JustSaying.Sample.Restaurant.OrderingApi;
using JustSaying.Sample.Restaurant.OrderingApi.Handlers;
using JustSaying.Sample.Restaurant.OrderingApi.Models;
using JustSaying.Messaging.Middleware.Tracing;
using Scalar.AspNetCore;

Console.Title = "OrderingApi";

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Configure tracing: UseParentSpan makes consumer spans children of producer spans
builder.Services.AddSingleton(new TracingOptions { UseParentSpan = true });
builder.Services.AddTransient<TracingMiddleware>();
builder.Services.AddTransient<TracingPublishMiddleware>();

var configuration = builder.Configuration;

builder.Services.AddJustSaying(config =>
{
    config.Client(x =>
    {
        if (configuration.HasAWSServiceUrl())
        {
            // The AWS client SDK allows specifying a custom HTTP endpoint.
            // For testing purposes it is useful to specify a value that
            // points to a docker image such as `localstack/localstack`
            x.WithServiceUri(configuration.GetAWSServiceUri())
                .WithAnonymousCredentials();
        }
        else
        {
            // The real AWS environment will require some means of authentication
            //x.WithBasicCredentials("###", "###");
            //x.WithSessionCredentials("###", "###", "###");
        }
    });
    config.Messaging(x =>
    {
        // Configures which AWS Region to operate in
        x.WithRegion(configuration.GetAWSRegion());
    });
    config.Subscriptions(x =>
    {
        // Creates the following if they do not already exist
        //  - a SQS queue of name `orderreadyevent`
        //  - a SQS queue of name `orderreadyevent_error`
        //  - a SNS topic of name `orderreadyevent`
        //  - a SNS topic subscription on topic 'orderreadyevent' and queue 'orderreadyevent'
        x.ForTopic<OrderReadyEvent>(cfg =>
            cfg.WithMiddlewareConfiguration(pipe =>
            {
                pipe.Use<TracingMiddleware>();
                pipe.UseDefaults<OrderReadyEvent>(typeof(OrderReadyEventHandler));
            }));
        x.ForTopic<OrderDeliveredEvent>(cfg =>
            cfg.WithMiddlewareConfiguration(pipe =>
            {
                pipe.Use<TracingMiddleware>();
                pipe.UseDefaults<OrderDeliveredEvent>(typeof(OrderDeliveredEventHandler));
            }));
    });
    config.Publications(x =>
    {
        // Creates the following if they do not already exist
        //  - a SNS topic of name `orderplacedevent`
        x.WithTopic<OrderPlacedEvent>();
        x.WithTopic<OrderOnItsWayEvent>();
        x.WithPublishMiddleware<TracingPublishMiddleware>();
    });
});

// Added a message handler for message type for 'OrderReadyEvent' on topic 'orderreadyevent' and queue 'orderreadyevent'
builder.Services.AddJustSayingHandler<OrderReadyEvent, OrderReadyEventHandler>();
builder.Services.AddJustSayingHandler<OrderDeliveredEvent, OrderDeliveredEventHandler>();

// Add a background service that is listening for messages related to the above subscriptions
builder.Services.AddHostedService<BusService>();

builder.Services.AddOpenApi();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapOpenApi();
app.MapScalarApiReference();

app.MapPost("api/orders",
    async (CustomerOrderModel order, IMessagePublisher publisher) =>
    {
        app.Logger.LogInformation("Order received for {Description}", order.Description);

        // Save order to database generating OrderId
        var orderId = Random.Shared.Next(1, 100);

        var message = new OrderPlacedEvent
        {
            OrderId = orderId,
            Description = order.Description
        };

        await publisher.PublishAsync(message, CancellationToken.None);

        app.Logger.LogInformation("Order {OrderId} placed", orderId);
    });

app.MapPost("api/multi-orders",
    async (IReadOnlyCollection<CustomerOrderModel> orders, IMessageBatchPublisher publisher) =>
    {
        app.Logger.LogInformation("Orders received: {Orders}", orders);

        // Save order to database generating OrderId
        var message = orders.Select(order =>
            {
                var orderId = Random.Shared.Next(1, 100);
                return new OrderPlacedEvent
                {
                    OrderId = orderId,
                    Description = order.Description
                };
            })
            .ToList();

        await publisher.PublishAsync(message);

        app.Logger.LogInformation("Order {OrderIds} placed", message.Select(x => x.OrderId));
    });

await app.RunAsync();
