---
---

# Testing

Testing JustSaying applications requires strategies for both unit testing (mocking) and integration testing (LocalStack). This guide covers both approaches.

## Unit Testing with Mocking

Mock `IMessagePublisher` and `IMessageBatchPublisher` to test application logic without AWS dependencies.

### Mocking IMessagePublisher

```csharp
public class OrderServiceTests
{
    [Fact]
    public async Task PlaceOrder_PublishesOrderPlacedEvent()
    {
        // Arrange
        var mockPublisher = new Mock<IMessagePublisher>();
        var service = new OrderService(mockPublisher.Object);

        // Act
        await service.PlaceOrder(123, "Test order");

        // Assert
        mockPublisher.Verify(
            x => x.PublishAsync(
                It.Is<OrderPlacedEvent>(e => e.OrderId == 123),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

### Mocking IMessageBatchPublisher

```csharp
[Fact]
public async Task PlaceBulkOrders_PublishesBatchOfEvents()
{
    // Arrange
    var mockBatchPublisher = new Mock<IMessageBatchPublisher>();
    var service = new OrderService(mockBatchPublisher.Object);

    var orders = new[] { "Order 1", "Order 2", "Order 3" };

    // Act
    await service.PlaceBulkOrders(orders);

    // Assert
    mockBatchPublisher.Verify(
        x => x.PublishAsync(
            It.Is<IEnumerable<OrderPlacedEvent>>(
                events => events.Count() == 3),
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### Mocking Message Handlers

Test handlers independently of JustSaying infrastructure:

```csharp
public class OrderPlacedEventHandlerTests
{
    [Fact]
    public async Task Handle_ProcessesOrder()
    {
        // Arrange
        var mockRepository = new Mock<IOrderRepository>();
        var handler = new OrderPlacedEventHandler(mockRepository.Object);

        var message = new OrderPlacedEvent
        {
            OrderId = 123,
            Description = "Test order"
        };

        // Act
        var result = await handler.Handle(message);

        // Assert
        Assert.True(result);
        mockRepository.Verify(
            x => x.SaveOrder(It.Is<Order>(o => o.Id == 123)),
            Times.Once);
    }
}
```

## Integration Testing with LocalStack

LocalStack provides local AWS services for integration testing.

### LocalStack Setup

#### Docker Compose

Create `docker-compose.test.yml`:

```yaml
version: '3.8'

services:
  localstack:
    image: localstack/localstack:latest
    ports:
      - "4566:4566"
    environment:
      - SERVICES=sns,sqs
      - DEBUG=1
      - DATA_DIR=/tmp/localstack/data
    volumes:
      - "./localstack-data:/tmp/localstack"
```

Start LocalStack:

```bash
docker-compose -f docker-compose.test.yml up -d
```

#### Test Configuration

Configure JustSaying to use LocalStack in tests:

```csharp
public class IntegrationTestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; }

    public IntegrationTestBase()
    {
        var services = new ServiceCollection();

        services.AddJustSaying(config =>
        {
            config.Client(x =>
            {
                x.WithServiceUri(new Uri("http://localhost:4566"))
                 .WithAnonymousCredentials();
            });

            config.Messaging(x => x.WithRegion("us-east-1"));

            config.Publications(x =>
            {
                x.WithTopic<TestOrderEvent>();
            });

            config.Subscriptions(x =>
            {
                x.ForTopic<TestOrderEvent>();
            });
        });

        services.AddJustSayingHandler<TestOrderEvent, TestOrderEventHandler>();
        services.AddHostedService<BusService>();

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        (ServiceProvider as IDisposable)?.Dispose();
    }
}
```

### Integration Test Example

```csharp
public class MessagePublishingTests : IntegrationTestBase
{
    [Fact]
    public async Task Can_Publish_And_Receive_Message()
    {
        // Arrange
        var publisher = ServiceProvider.GetRequiredService<IMessagePublisher>();
        var handler = ServiceProvider.GetRequiredService<TestOrderEventHandler>();

        var message = new TestOrderEvent
        {
            OrderId = 123,
            Description = "Integration test order"
        };

        // Act
        await publisher.PublishAsync(message);

        // Wait for message to be received
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(handler.HandledMessages.ContainsKey(123));
    }
}
```

### Test Fixtures for xUnit

Use xUnit collection fixtures to share LocalStack across tests:

```csharp
[CollectionDefinition("LocalStack")]
public class LocalStackCollection : ICollectionFixture<LocalStackFixture>
{
}

public class LocalStackFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }

    public LocalStackFixture()
    {
        // Start LocalStack container
        // Configure JustSaying
        // Build service provider
    }

    public void Dispose()
    {
        // Clean up resources
    }
}

[Collection("LocalStack")]
public class MyIntegrationTests
{
    private readonly LocalStackFixture _fixture;

    public MyIntegrationTests(LocalStackFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Test_WithSharedLocalStack()
    {
        // Use _fixture.ServiceProvider
    }
}
```

## Testcontainers

Use Testcontainers for automatic LocalStack management:

```csharp
public class TestcontainersTests : IAsyncLifetime
{
    private LocalStackContainer _localStack;
    private IServiceProvider _serviceProvider;

    public async Task InitializeAsync()
    {
        _localStack = new LocalStackBuilder()
            .WithImage("localstack/localstack:latest")
            .WithEnvironment("SERVICES", "sns,sqs")
            .Build();

        await _localStack.StartAsync();

        var services = new ServiceCollection();

        services.AddJustSaying(config =>
        {
            config.Client(x =>
            {
                x.WithServiceUri(_localStack.GetConnectionString())
                 .WithAnonymousCredentials();
            });

            config.Messaging(x => x.WithRegion("us-east-1"));
            config.Publications(x => x.WithTopic<TestEvent>());
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Can_Publish_Message()
    {
        var publisher = _serviceProvider.GetRequiredService<IMessagePublisher>();
        await publisher.PublishAsync(new TestEvent { Id = 1 });
    }

    public async Task DisposeAsync()
    {
        await _localStack.DisposeAsync();
    }
}
```

## Best Practices

### Unit Tests

1. **Mock infrastructure**: Mock `IMessagePublisher` to test business logic
2. **Test handlers independently**: Handlers should have minimal dependencies
3. **Verify message content**: Assert on specific message properties
4. **Test error scenarios**: Verify error handling in handlers

### Integration Tests

1. **Use LocalStack**: Don't hit real AWS services in tests
2. **Isolate tests**: Each test should use unique topic/queue names
3. **Clean up resources**: Delete topics/queues after tests
4. **Use test fixtures**: Share LocalStack across tests for performance
5. **Add timeouts**: Message delivery isn't instantaneous

### Test Data

1. **Use realistic data**: Test with production-like message sizes and formats
2. **Test edge cases**: Null values, empty strings, maximum sizes
3. **Test compression**: If using compression, test with large messages
4. **Test encryption**: If using KMS, test with encrypted messages

## Common Patterns

### Waiting for Messages

Messages aren't delivered instantly. Use polling or timeouts:

```csharp
// Poll for message receipt
var sw = Stopwatch.StartNew();
while (sw.Elapsed < TimeSpan.FromSeconds(10))
{
    if (handler.ReceivedMessages.Count > 0)
        break;
    await Task.Delay(100);
}

Assert.NotEmpty(handler.ReceivedMessages);
```

### Unique Test Resources

Generate unique topic/queue names per test:

```csharp
public class MyTests
{
    private readonly string _topicSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

    [Fact]
    public async Task Test_WithUniqueResources()
    {
        services.AddJustSaying(config =>
        {
            config.Publications(x =>
            {
                x.WithTopic<TestEvent>(cfg =>
                {
                    cfg.WithTopicName($"test-topic-{_topicSuffix}");
                });
            });
        });
    }
}
```

### Capturing Handler Execution

Create a test handler that captures calls:

```csharp
public class CaptureHandler<T> : IHandlerAsync<T> where T : Message
{
    public List<T> HandledMessages { get; } = new();

    public Task<bool> Handle(T message)
    {
        HandledMessages.Add(message);
        return Task.FromResult(true);
    }
}

// In test
services.AddSingleton<IHandlerAsync<TestEvent>, CaptureHandler<TestEvent>>();
```

## Troubleshooting

### LocalStack not starting

Check Docker logs:

```bash
docker logs justsaying-localstack-1
```

Verify LocalStack is running:

```bash
curl http://localhost:4566/_localstack/health
```

### Messages not being received

1. Verify LocalStack is running
2. Check topic and queue were created
3. Ensure subscription was established
4. Add logging to handler
5. Increase wait time in tests

### Tests are slow

1. Use test fixtures to share LocalStack
2. Run tests in parallel where possible
3. Reduce message polling intervals
4. Use smaller test datasets

## See Also

- [Service Endpoints](../aws-configuration/service-endpoints.md) - LocalStack configuration
- [Sample Application](../sample-application.md) - Example integration tests
- [LocalStack Documentation](https://docs.localstack.cloud/) - LocalStack details
