using JustSaying.Extensions.Kafka;
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
            // Subscribe to Kafka topics with retry and DLT configuration
            x.ForKafka<OrderPlacedEvent>("order-placed", kafka =>
            {
                kafka.WithGroupId(configuration.GetKafkaConsumerGroup());
                
                // Configure in-process retry with exponential backoff
                // This is the cost-optimized option (no additional topics needed)
                // - Retries up to 3 times
                // - Starts with 1 second delay, doubles each time (exponential backoff)
                // - Maximum delay capped at 30 seconds
                kafka.WithInProcessRetry(
                    maxAttempts: 3,
                    initialBackoff: TimeSpan.FromSeconds(1),
                    exponentialBackoff: true,
                    maxBackoff: TimeSpan.FromSeconds(30));
                
                // Configure Dead Letter Topic for messages that fail all retries
                kafka.WithDeadLetterTopic("order-placed-dlt");
            });
            
            // Alternative: Topic Chaining retry (higher throughput, but uses more topics)
            // Uncomment below to see this pattern instead:
            /*
            x.ForKafka<OrderPlacedEvent>("order-placed", kafka =>
            {
                kafka.WithGroupId(configuration.GetKafkaConsumerGroup());
                
                // Topic chaining creates separate retry topics for non-blocking retries
                kafka.WithTopicChainingRetry(
                    maxAttempts: 3,
                    initialBackoff: TimeSpan.FromSeconds(5),
                    exponentialBackoff: true)
                .WithRetryTopic("order-placed-retry")
                .WithDeadLetterTopic("order-placed-dlt");
            });
            */
            
            x.ForKafka<OrderConfirmedEvent>("order-confirmed", kafka =>
            {
                kafka.WithGroupId(configuration.GetKafkaConsumerGroup());
                
                // Simple retry configuration for confirmed events
                kafka.WithInProcessRetry(
                    maxAttempts: 2,
                    initialBackoff: TimeSpan.FromMilliseconds(500));
                kafka.WithDeadLetterTopic("order-confirmed-dlt");
            });
        });
        
        config.Publications(x =>
        {
            // Publish to Kafka topics - inherits global Kafka configuration
            x.WithKafka<OrderPlacedEvent>("order-placed");
            x.WithKafka<OrderConfirmedEvent>("order-confirmed");
        });
    });
    
    // Add OpenTelemetry metrics and distributed tracing for Kafka
    builder.Services.AddKafkaOpenTelemetry();

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
            Description = @"Sample API demonstrating JustSaying with Kafka transport, retry mechanism, and Dead Letter Topics (DLT).

## Testing Retry and DLT Behavior

**Normal Processing (Amount <= $500):**
- Orders process successfully on first attempt

**Transient Failures with Retry (Amount $501-$1000):**
- 70% chance of transient failure on first attempts
- Will retry up to 3 times with exponential backoff
- Usually succeeds after 1-2 retries

**Permanent Failures to DLT (Amount > $1000):**
- Always fails processing
- Retried 3 times, then sent to Dead Letter Topic (order-placed-dlt)
- Monitor the DLT topic to see failed messages

## Example Requests

**Normal order:** `{""customerId"": ""C001"", ""amount"": 99.99, ...}`
**Retry scenario:** `{""customerId"": ""C002"", ""amount"": 750.00, ...}`
**DLT scenario:** `{""customerId"": ""C003"", ""amount"": 1500.00, ...}`"
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
    
    // Test endpoints for demonstrating retry and DLT behavior
    app.MapPost("api/orders/test/normal",
        async (IMessagePublisher publisher) =>
        {
            var orderId = $"ORD-{Random.Shared.Next(1, 10000):D5}";
            var message = new OrderPlacedEvent
            {
                OrderId = orderId,
                CustomerId = "TEST-NORMAL",
                Amount = 99.99m, // Below $500 - will succeed immediately
                OrderDate = DateTime.UtcNow,
                RaisingComponent = appName,
                Tenant = "sample-tenant",
                Items = [new OrderItem { ProductId = "P001", ProductName = "Test Product", Quantity = 1, UnitPrice = 99.99m }]
            };
            await publisher.PublishAsync(message);
            return Results.Ok(new { orderId, scenario = "Normal processing", expectedBehavior = "Succeeds on first attempt" });
        })
        .WithName("TestNormalOrder")
        .WithDescription("Creates an order that processes successfully on first attempt");
    
    app.MapPost("api/orders/test/retry",
        async (IMessagePublisher publisher) =>
        {
            var orderId = $"ORD-{Random.Shared.Next(1, 10000):D5}";
            var message = new OrderPlacedEvent
            {
                OrderId = orderId,
                CustomerId = "TEST-RETRY",
                Amount = 750.00m, // Between $500-$1000 - will likely fail then succeed after retries
                OrderDate = DateTime.UtcNow,
                RaisingComponent = appName,
                Tenant = "sample-tenant",
                Items = [new OrderItem { ProductId = "P002", ProductName = "Premium Product", Quantity = 1, UnitPrice = 750.00m }]
            };
            await publisher.PublishAsync(message);
            return Results.Ok(new { orderId, scenario = "Transient failure with retry", expectedBehavior = "70% chance of failure, retries up to 3 times" });
        })
        .WithName("TestRetryOrder")
        .WithDescription("Creates an order that has transient failures and demonstrates retry behavior");
    
    app.MapPost("api/orders/test/dlt",
        async (IMessagePublisher publisher) =>
        {
            var orderId = $"ORD-{Random.Shared.Next(1, 10000):D5}";
            var message = new OrderPlacedEvent
            {
                OrderId = orderId,
                CustomerId = "TEST-DLT",
                Amount = 1500.00m, // Above $1000 - will always fail and go to DLT
                OrderDate = DateTime.UtcNow,
                RaisingComponent = appName,
                Tenant = "sample-tenant",
                Items = [new OrderItem { ProductId = "P003", ProductName = "Luxury Product", Quantity = 1, UnitPrice = 1500.00m }]
            };
            await publisher.PublishAsync(message);
            return Results.Ok(new { 
                orderId, 
                scenario = "Permanent failure to DLT", 
                expectedBehavior = "Always fails, retries 3 times, then sent to order-placed-dlt topic",
                dltTopic = "order-placed-dlt"
            });
        })
        .WithName("TestDltOrder")
        .WithDescription("Creates an order that always fails and gets sent to the Dead Letter Topic");

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

