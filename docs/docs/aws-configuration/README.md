---
---

# AWS Configuration

JustSaying requires AWS configuration to connect to SNS and SQS services. Configure AWS settings using the fluent API within `AddJustSaying` to specify credentials, regions, and service endpoints.

## Configuration Areas

AWS configuration in JustSaying is divided into two main areas:

### Client Configuration

The `.Client(...)` method configures AWS SDK client settings including credentials and service endpoints.

```csharp
services.AddJustSaying(config =>
{
    config.Client(x =>
    {
        // AWS client configuration
        x.WithAnonymousCredentials();
        x.WithServiceUri(new Uri("http://localhost:4566"));
    });
});
```

### Messaging Configuration

The `.Messaging(...)` method configures region and messaging-specific settings.

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x =>
    {
        // Region configuration
        x.WithRegion("us-east-1");
    });
});
```

## Complete Example

Here's a complete example showing both client and messaging configuration:

```csharp
services.AddJustSaying(config =>
{
    config.Client(x =>
    {
        if (hostEnvironment.IsDevelopment())
        {
            // LocalStack for local development
            x.WithServiceUri(new Uri("http://localhost:4566"))
             .WithAnonymousCredentials();
        }
        // Production uses IAM roles automatically (no configuration needed)
    });

    config.Messaging(x =>
    {
        x.WithRegion(configuration.GetValue<string>("AWS:Region"));
    });

    config.Publications(x =>
    {
        x.WithTopic<OrderPlacedEvent>();
    });

    config.Subscriptions(x =>
    {
        x.ForTopic<OrderPlacedEvent>();
    });
});
```

## Configuration Topics

- [Credentials](credentials.md) - Configuring AWS credentials for authentication
- [Regions](regions.md) - Specifying AWS regions for your resources
- [Service Endpoints](service-endpoints.md) - Custom endpoints for LocalStack and testing

## IAM Permissions

For information about the IAM permissions required by JustSaying, see [AWS IAM](../aws-iam.md).
