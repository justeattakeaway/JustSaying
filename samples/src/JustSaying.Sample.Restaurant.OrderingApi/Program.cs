using JustSaying.Messaging;
using JustSaying.Sample.Restaurant.Models;
using JustSaying.Sample.Restaurant.OrderingApi;
using JustSaying.Sample.Restaurant.OrderingApi.Handlers;
using JustSaying.Sample.Restaurant.OrderingApi.Models;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

const string appName = "OrderingApi";
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Debug()
    .Enrich.WithProperty("AppName", appName)
    .CreateLogger();

Console.Title = "OrderingApi";

try
{
    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;
    builder.Host.UseSerilog();
    builder.Services.AddJustSaying(config =>
    {
        config.Client(x =>
        {
            if (configuration.HasAWSServiceUrl())
            {
                // The AWS client SDK allows specifying a custom HTTP endpoint.
                // For testing purposes it is useful to specify a value that
                // points to a docker image such as `p4tin/goaws` or `localstack/localstack`
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
            x.ForTopic<OrderReadyEvent>();
            x.ForTopic<OrderDeliveredEvent>();
        });
        config.Publications(x =>
        {
            // Creates the following if they do not already exist
            //  - a SNS topic of name `orderplacedevent`
            x.WithTopic<OrderPlacedEvent>();
            x.WithTopic<OrderOnItsWayEvent>();
        });
    });

    // Added a message handler for message type for 'OrderReadyEvent' on topic 'orderreadyevent' and queue 'orderreadyevent'
    builder.Services.AddJustSayingHandler<OrderReadyEvent, OrderReadyEventHandler>();
    builder.Services.AddJustSayingHandler<OrderDeliveredEvent, OrderDeliveredEventHandler>();

    // Add a background service that is listening for messages related to the above subscriptions
    builder.Services.AddHostedService<BusService>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Restaurant Ordering API", Version = "v1" });
    });

    var app = builder.Build();
    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant Ordering API");
        c.RoutePrefix = string.Empty;
    });

    app.MapPost("api/orders",
        async (CustomerOrderModel order, IMessagePublisher publisher) =>
        {
            app.Logger.LogInformation("Order received for {description}", order.Description);

            // Save order to database generating OrderId
            var orderId = new Random().Next(1, 100);

            var message = new OrderPlacedEvent
            {
                OrderId = orderId,
                Description = order.Description
            };

            await publisher.PublishAsync(message);

            app.Logger.LogInformation("Order {orderId} placed", orderId);
        });

    await app.RunAsync();
}
catch (Exception e)
{
    Log.Fatal(e, "Error occurred during startup: {Message}", e.Message);
}
finally
{
    Log.CloseAndFlush();
}
