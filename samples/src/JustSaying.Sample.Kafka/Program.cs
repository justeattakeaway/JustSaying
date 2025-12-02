using JustSaying;
using JustSaying.Extensions.Kafka;
using JustSaying.Messaging;
using JustSaying.Sample.Kafka.Handlers;
using JustSaying.Sample.Kafka.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace JustSaying.Sample.Kafka;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Add logging
                    services.AddLogging(builder =>
                    {
                        builder.AddSerilog(dispose: true);
                    });

                    // Register message handlers
                    services.AddSingleton<OrderPlacedEventHandler>();
                    services.AddSingleton<OrderConfirmedEventHandler>();

                    // Configure JustSaying with Kafka
                    services.AddJustSaying(config =>
                    {
                        config.Messaging(messaging =>
                        {
                            // Not using AWS for this Kafka example
                            messaging.WithRegion("us-east-1");
                        });

                        // Add Kafka publishers with CloudEvents support
                        config.WithKafkaPublisher<OrderPlacedEvent>("order-placed", kafka =>
                        {
                            kafka.BootstrapServers = "localhost:9092";
                            kafka.EnableCloudEvents = true;
                            kafka.CloudEventsSource = "urn:justsaying:sample:orders";
                        });

                        config.WithKafkaPublisher<OrderConfirmedEvent>("order-confirmed", kafka =>
                        {
                            kafka.BootstrapServers = "localhost:9092";
                            kafka.EnableCloudEvents = true;
                            kafka.CloudEventsSource = "urn:justsaying:sample:orders";
                        });
                    });

                    // Add background services
                    services.AddHostedService<PublisherService>();
                    services.AddHostedService<ConsumerService>();
                })
                .Build();

            Log.Information("Starting Kafka Sample Application with CloudEvents support");
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

/// <summary>
/// Background service that publishes sample messages to Kafka.
/// </summary>
public class PublisherService : BackgroundService
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<PublisherService> _logger;

    public PublisherService(IMessagePublisher publisher, ILogger<PublisherService> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit for consumers to start
        await Task.Delay(2000, stoppingToken);

        var orderNumber = 1;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Publish OrderPlacedEvent
                var orderPlaced = new OrderPlacedEvent
                {
                    OrderId = $"ORD-{orderNumber:D5}",
                    CustomerId = $"CUST-{Random.Shared.Next(1, 100):D3}",
                    Amount = Random.Shared.Next(10, 1000),
                    OrderDate = DateTime.UtcNow,
                    RaisingComponent = "OrderService",
                    Tenant = "tenant-demo",
                    Items = new List<OrderItem>
                    {
                        new() { ProductId = "PROD-001", ProductName = "Widget", Quantity = 2, UnitPrice = 25.00m },
                        new() { ProductId = "PROD-002", ProductName = "Gadget", Quantity = 1, UnitPrice = 50.00m }
                    }
                };

                await _publisher.PublishAsync(orderPlaced, stoppingToken);
                _logger.LogInformation("Published OrderPlacedEvent for {OrderId}", orderPlaced.OrderId);

                // Wait a bit
                await Task.Delay(1000, stoppingToken);

                // Publish OrderConfirmedEvent
                var orderConfirmed = new OrderConfirmedEvent
                {
                    OrderId = orderPlaced.OrderId,
                    ConfirmedAt = DateTime.UtcNow,
                    ConfirmedBy = "AutomatedSystem",
                    RaisingComponent = "OrderService",
                    Tenant = "tenant-demo"
                };

                await _publisher.PublishAsync(orderConfirmed, stoppingToken);
                _logger.LogInformation("Published OrderConfirmedEvent for {OrderId}", orderConfirmed.OrderId);

                orderNumber++;

                // Publish every 5 seconds
                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing messages");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

/// <summary>
/// Background service that consumes messages from Kafka.
/// </summary>
public class ConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsumerService> _logger;
    private KafkaMessageConsumer _orderPlacedConsumer;
    private KafkaMessageConsumer _orderConfirmedConsumer;

    public ConsumerService(IServiceProvider serviceProvider, ILogger<ConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Create consumers
            _orderPlacedConsumer = _serviceProvider.CreateKafkaConsumer("order-placed", kafka =>
            {
                kafka.BootstrapServers = "localhost:9092";
                kafka.GroupId = "sample-consumer-group";
                kafka.EnableCloudEvents = true;
            });

            _orderConfirmedConsumer = _serviceProvider.CreateKafkaConsumer("order-confirmed", kafka =>
            {
                kafka.BootstrapServers = "localhost:9092";
                kafka.GroupId = "sample-consumer-group";
                kafka.EnableCloudEvents = true;
            });

            var orderPlacedHandler = _serviceProvider.GetRequiredService<OrderPlacedEventHandler>();
            var orderConfirmedHandler = _serviceProvider.GetRequiredService<OrderConfirmedEventHandler>();

            _logger.LogInformation("Starting Kafka consumers with CloudEvents support");

            // Start both consumers
            var tasks = new[]
            {
                _orderPlacedConsumer.StartAsync(orderPlacedHandler, stoppingToken),
                _orderConfirmedConsumer.StartAsync(orderConfirmedHandler, stoppingToken)
            };

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in consumer service");
        }
    }

    public override void Dispose()
    {
        _orderPlacedConsumer?.Dispose();
        _orderConfirmedConsumer?.Dispose();
        base.Dispose();
    }
}
