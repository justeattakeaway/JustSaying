---
---

# Service Endpoints

Service endpoints specify the URL where AWS services can be reached. By default, the AWS SDK connects to the real AWS services. Configure custom service endpoints for testing with LocalStack or other AWS-compatible services.

## Default Behavior

If you don't configure a service endpoint, the AWS SDK connects to the real AWS SNS and SQS services in the region you specified.

## Configuration Methods

### `WithServiceUrl(string url)`

Specify a custom service endpoint URL as a string.

```csharp
config.Client(x =>
{
    x.WithServiceUrl("http://localhost:4566");
});
```

### `WithServiceUri(Uri uri)`

Specify a custom service endpoint using a `Uri` object.

```csharp
config.Client(x =>
{
    x.WithServiceUri(new Uri("http://localhost:4566"));
});
```

## LocalStack Configuration

[LocalStack](https://localstack.cloud/) provides a local AWS cloud stack for development and testing. Configure JustSaying to use LocalStack:

```csharp
services.AddJustSaying(config =>
{
    config.Client(x =>
    {
        x.WithServiceUri(new Uri("http://localhost:4566"))
         .WithAnonymousCredentials();
    });

    config.Messaging(x =>
    {
        x.WithRegion("us-east-1"); // LocalStack default region
    });

    config.Publications(x =>
    {
        x.WithTopic<OrderPlacedEvent>();
    });
});
```

**Note**: LocalStack uses port `4566` for all services by default.

## Environment-Specific Configuration

Use different configurations for different environments:

```csharp
services.AddJustSaying(config =>
{
    config.Client(x =>
    {
        if (hostEnvironment.IsDevelopment())
        {
            // LocalStack (local development)
            x.WithServiceUri(new Uri("http://localhost:4566"))
             .WithAnonymousCredentials();
        }
        else
        {
            // Production uses real AWS (no service URL configuration)
            // IAM role provides credentials automatically
        }
    });

    config.Messaging(x =>
    {
        x.WithRegion(configuration.GetValue<string>("AWS:Region"));
    });
});
```

## Configuration from Settings

Store the LocalStack URL in configuration:

**appsettings.Development.json**:
```json
{
  "AWS": {
    "ServiceUrl": "http://localhost:4566",
    "Region": "us-east-1"
  }
}
```

**Startup configuration**:
```csharp
services.AddJustSaying(config =>
{
    config.Client(x =>
    {
        var serviceUrl = configuration.GetValue<string>("AWS:ServiceUrl");
        if (!string.IsNullOrEmpty(serviceUrl))
        {
            x.WithServiceUri(new Uri(serviceUrl))
             .WithAnonymousCredentials();
        }
    });

    config.Messaging(x =>
    {
        x.WithRegion(configuration.GetValue<string>("AWS:Region"));
    });
});
```

## Docker Compose Setup

Run LocalStack using Docker Compose for local development:

**docker-compose.yml**:
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
      - "/var/run/docker.sock:/var/run/docker.sock"
```

Start LocalStack:
```bash
docker-compose up -d
```

Your JustSaying application can now connect to LocalStack at `http://localhost:4566`.

## Testing Considerations

### Integration Tests

Configure tests to use LocalStack:

```csharp
[Collection("LocalStack")]
public class MessagePublishingTests
{
    private readonly IServiceProvider _serviceProvider;

    public MessagePublishingTests()
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
            config.Publications(x => x.WithTopic<TestEvent>());
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task Can_Publish_Message_To_LocalStack()
    {
        var publisher = _serviceProvider.GetRequiredService<IMessagePublisher>();
        await publisher.PublishAsync(new TestEvent { Id = 123 });
        // Assert
    }
}
```

### Testcontainers

Use [Testcontainers](https://dotnet.testcontainers.org/) to automatically manage LocalStack containers in tests:

```csharp
var localStackContainer = new ContainerBuilder()
    .WithImage("localstack/localstack:latest")
    .WithPortBinding(4566, 4566)
    .WithEnvironment("SERVICES", "sns,sqs")
    .Build();

await localStackContainer.StartAsync();

services.AddJustSaying(config =>
{
    config.Client(x =>
    {
        x.WithServiceUri(new Uri("http://localhost:4566"))
         .WithAnonymousCredentials();
    });
});
```

## Custom AWS-Compatible Services

Service endpoints can be used with any AWS-compatible service that implements the SNS and SQS APIs:

- LocalStack (local development)
- ElasticMQ (SQS-compatible message queue)
- Custom AWS service implementations

Configure the endpoint URL to point to your custom service.

## Troubleshooting

### "Unable to connect to the remote server"

Verify that:
1. LocalStack is running: `docker ps`
2. The service URL is correct
3. The port is accessible from your application

### Topics/Queues not persisting

By default, LocalStack doesn't persist data. Mount a volume to persist data:

```yaml
volumes:
  - "./localstack-data:/tmp/localstack"
```

### Region mismatch errors

Ensure the region in `.WithRegion(...)` matches the LocalStack configuration. LocalStack uses `us-east-1` by default.

## See Also

- [Credentials](credentials.md) - Anonymous credentials for LocalStack
- [Testing](../advanced/testing.md) - Testing strategies with JustSaying
- [LocalStack Documentation](https://docs.localstack.cloud/) - LocalStack setup and configuration
