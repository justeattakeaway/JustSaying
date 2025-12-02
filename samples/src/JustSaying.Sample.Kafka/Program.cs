using JustSaying.Extensions.Kafka.Fluent;
using JustSaying.Messaging;
using JustSaying.Sample.Kafka;
using JustSaying.Sample.Kafka.Handlers;
using JustSaying.Sample.Kafka.Messages;
using JustSaying.Sample.Kafka.Models;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

const string appName = "KafkaOrderingApi";
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Debug()
    .Enrich.WithProperty("AppName", appName)
    .CreateLogger();

Console.Title = "Kafka Ordering API";

try
{
    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;
    builder.Host.UseSerilog();
    
    builder.Services.AddJustSaying(config =>
    {
        config.Messaging(x =>
        {
            // Configure AWS region (required for compatibility even when using only Kafka)
            x.WithRegion(configuration.GetAWSRegion());
            
            // Configure global Kafka settings
            x.WithKafka(kafka =>
            {
                kafka.BootstrapServers = configuration.GetKafkaBootstrapServers();
                kafka.EnableCloudEvents = true;
                kafka.CloudEventsSource = "urn:justsaying:sample:orders";
            });
        });
        
        config.Subscriptions(x =>
        {
            // Subscribe to Kafka topics - inherits global Kafka configuration
            x.ForKafka<OrderPlacedEvent>("order-placed", kafka =>
            {
                kafka.WithGroupId(configuration.GetKafkaConsumerGroup());
            });
            
            x.ForKafka<OrderConfirmedEvent>("order-confirmed", kafka =>
            {
                kafka.WithGroupId(configuration.GetKafkaConsumerGroup());
            });
        });
        
        config.Publications(x =>
        {
            // Publish to Kafka topics - inherits global Kafka configuration
            x.WithKafka<OrderPlacedEvent>("order-placed");
            x.WithKafka<OrderConfirmedEvent>("order-confirmed");
        });
    });

    // Register message handlers
    builder.Services.AddJustSayingHandler<OrderPlacedEvent, OrderPlacedEventHandler>();
    builder.Services.AddJustSayingHandler<OrderConfirmedEvent, OrderConfirmedEventHandler>();

    // Add background service that starts the bus and listens for messages
    builder.Services.AddHostedService<BusService>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Kafka Ordering API",
            Version = "v1",
            Description = "Sample API demonstrating JustSaying with Kafka transport"
        });
    });

    var app = builder.Build();
    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kafka Ordering API");
        c.RoutePrefix = string.Empty;
    });

    app.MapPost("api/orders",
        async (CustomerOrderModel order, IMessagePublisher publisher) =>
        {
            app.Logger.LogInformation("Order received for {CustomerId}: {Description}", 
                order.CustomerId, order.Description);

            // Simulate saving order to database and generating OrderId
            var orderId = $"ORD-{Random.Shared.Next(1, 10000):D5}";

            var message = new OrderPlacedEvent
            {
                OrderId = orderId,
                CustomerId = order.CustomerId,
                Amount = order.Amount,
                OrderDate = DateTime.UtcNow,
                RaisingComponent = appName,
                Tenant = "sample-tenant",
                Items = order.Items.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList()
            };

            await publisher.PublishAsync(message);

            app.Logger.LogInformation("Order {OrderId} placed successfully", orderId);
            
            return Results.Ok(new { orderId, message = "Order placed successfully" });
        })
        .WithName("PlaceOrder");

    app.MapGet("api/health",
        () => Results.Ok(new { status = "healthy", service = appName, timestamp = DateTime.UtcNow }))
        .WithName("HealthCheck");

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

