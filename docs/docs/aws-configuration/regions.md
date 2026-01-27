---
---

# Regions

AWS regions must be configured in JustSaying to specify where your SNS topics and SQS queues are located. Region configuration is required and is set using the `.Messaging(...)` fluent API.

## Why Region Configuration is Required

SNS topics and SQS queues are regional resources in AWS. You must specify which AWS region to use so that JustSaying knows where to create or access your messaging infrastructure.

## Configuration Methods

### `WithRegion(string region)`

Specify the AWS region using a region code string.

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x =>
    {
        x.WithRegion("us-east-1");
    });
});
```

**Common Region Codes**:
- `us-east-1` - US East (Virginia)
- `us-west-2` - US West (Oregon)
- `eu-west-1` - Europe (Ireland)
- `eu-central-1` - Europe (Frankfurt)
- `ap-southeast-1` - Asia Pacific (Singapore)
- `ap-northeast-1` - Asia Pacific (Tokyo)

See the [AWS Regions documentation](https://docs.aws.amazon.com/general/latest/gr/rande.html) for a complete list.

### `WithRegion(RegionEndpoint region)`

Specify the AWS region using the AWS SDK's `RegionEndpoint` enum. This provides compile-time safety and IntelliSense support.

```csharp
using Amazon;

services.AddJustSaying(config =>
{
    config.Messaging(x =>
    {
        x.WithRegion(RegionEndpoint.USEast1);
    });
});
```

## Configuration Patterns

### From Configuration

Store the region in your application configuration (`appsettings.json`, environment variables, etc.):

**appsettings.json**:
```json
{
  "AWS": {
    "Region": "us-east-1"
  }
}
```

**Startup configuration**:
```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x =>
    {
        x.WithRegion(configuration.GetValue<string>("AWS:Region"));
    });
});
```

### Environment-Specific Regions

Use different regions for different environments:

```csharp
services.AddJustSaying(config =>
{
    config.Messaging(x =>
    {
        var region = hostEnvironment.IsProduction()
            ? "us-east-1"  // Production in US East
            : "us-west-2"; // Development in US West

        x.WithRegion(region);
    });
});
```

### LocalStack Configuration

LocalStack typically uses `us-east-1` as the default region:

```csharp
services.AddJustSaying(config =>
{
    config.Client(x =>
    {
        if (hostEnvironment.IsDevelopment())
        {
            x.WithServiceUri(new Uri("http://localhost:4566"))
             .WithAnonymousCredentials();
        }
    });

    config.Messaging(x =>
    {
        // LocalStack default region
        x.WithRegion("us-east-1");
    });
});
```

## Multi-Region Considerations

JustSaying currently supports a single region per `MessagingBus` instance. If you need to publish to or subscribe from multiple regions, you must create separate bus instances for each region:

```csharp
// US East bus
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("us-east-1"));
    config.Publications(x => x.WithTopic<USOrderEvent>());
});

// EU West bus (requires additional configuration to avoid conflicts)
// Note: Multiple bus instances require careful service registration
```

For most applications, a single region is sufficient. Cross-region messaging can be implemented at the application level if needed.

## Region and Credentials

The region configuration is independent of credentials configuration:

```csharp
services.AddJustSaying(config =>
{
    // Credentials (optional - uses IAM role by default)
    config.Client(x =>
    {
        x.WithBasicCredentials(accessKey, secretKey);
    });

    // Region (required)
    config.Messaging(x =>
    {
        x.WithRegion("eu-west-1");
    });
});
```

## Common Errors

### "No region endpoint specified"

This error occurs when the region is not configured. Ensure you call `.WithRegion(...)` in your messaging configuration:

```csharp
config.Messaging(x =>
{
    x.WithRegion("us-east-1"); // Required
});
```

### "The security token included in the request is invalid"

This error may occur when credentials are valid for one region but you're trying to access resources in a different region. Verify that:

1. The region specified in `.WithRegion(...)` matches where your resources are located
2. Your IAM credentials have permissions for the specified region

## See Also

- [Credentials](credentials.md) - Configuring AWS credentials
- [Service Endpoints](service-endpoints.md) - Custom endpoints for LocalStack
- [AWS Regions and Endpoints](https://docs.aws.amazon.com/general/latest/gr/rande.html) - AWS documentation
