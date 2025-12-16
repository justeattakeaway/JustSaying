---
---

# Credentials

JustSaying uses the AWS SDK to communicate with SNS and SQS. Configure credentials using the `.Client(...)` fluent API to authenticate with AWS services.

## Default Behavior

If you don't explicitly configure credentials, the AWS SDK uses its [default credential provider chain](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/creds-assign.html), which checks:

1. Environment variables \(`AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`\)
2. AWS credentials file \(`~/.aws/credentials`\)
3. IAM role for EC2 instances, ECS tasks, or Lambda functions

For production applications running on AWS infrastructure \(EC2, ECS, Lambda\), **IAM roles are the recommended approach** and require no explicit configuration in JustSaying.

```csharp
services.AddJustSaying(config =>
{
    config.Client(x =>
    {
        // No credential configuration needed
        // AWS SDK will use IAM role automatically
    });

    config.Messaging(x => x.WithRegion("us-east-1"));
});
```

## Credential Configuration Methods

### `WithAnonymousCredentials()`

Uses anonymous credentials with no authentication. **Only use this for LocalStack or other testing scenarios.** Never use anonymous credentials with real AWS services.

```csharp
config.Client(x =>
{
    x.WithAnonymousCredentials();
});
```

**Use Case**: Local development with LocalStack

```csharp
config.Client(x =>
{
    if (hostEnvironment.IsDevelopment())
    {
        x.WithServiceUri(new Uri("http://localhost:4566"))
         .WithAnonymousCredentials();
    }
});
```

### `WithBasicCredentials(string accessKey, string secretKey)`

Uses explicit AWS access key and secret key. **Not recommended for production** as it requires hardcoding or storing credentials in configuration.

```csharp
config.Client(x =>
{
    x.WithBasicCredentials(
        configuration["AWS:AccessKey"],
        configuration["AWS:SecretKey"]
    );
});
```

**Security Warning**: Never commit credentials to source control. If you must use this method, retrieve credentials from a secure configuration source like Azure Key Vault, AWS Secrets Manager, or environment variables.

### `WithSessionCredentials(string accessKeyId, string secretAccessKey, string token)`

Uses temporary AWS credentials with a session token. This is typically used with AWS Security Token Service \(STS\) for temporary access.

```csharp
config.Client(x =>
{
    x.WithSessionCredentials(
        configuration["AWS:AccessKeyId"],
        configuration["AWS:SecretAccessKey"],
        configuration["AWS:SessionToken"]
    );
});
```

**Use Case**: Applications that assume IAM roles using STS or need temporary elevated permissions.

### `WithCredentials(AWSCredentials credentials)`

Uses a custom `AWSCredentials` object. This allows you to use any credentials provider supported by the AWS SDK.

```csharp
var credentials = new StoredProfileAWSCredentials("my-profile");

config.Client(x =>
{
    x.WithCredentials(credentials);
});
```

**Use Case**: Advanced scenarios requiring custom credential providers or profiles.

## Best Practices

### Production Applications

**On AWS Infrastructure \(Recommended\)**:
- Use IAM roles attached to EC2 instances, ECS tasks, or Lambda functions
- No explicit credential configuration needed in JustSaying
- Credentials are automatically rotated by AWS
- No risk of credential leakage

```csharp
// Production configuration - IAM role used automatically
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("us-east-1"));
    config.Publications(x => x.WithTopic<OrderPlacedEvent>());
});
```

**Outside AWS Infrastructure**:
- Use environment variables or secure configuration stores
- Consider AWS SSO or IAM Identity Center for user-based access
- Use `WithSessionCredentials` with temporary credentials from STS

### Development and Testing

**LocalStack**:
```csharp
config.Client(x =>
{
    x.WithServiceUri(new Uri("http://localhost:4566"))
     .WithAnonymousCredentials();
});
```

**AWS Developer Credentials**:
```csharp
// Uses ~/.aws/credentials file automatically
services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("us-east-1"));
});
```

**Named Profiles**:
```csharp
var credentials = new StoredProfileAWSCredentials("development");
config.Client(x =>
{
    x.WithCredentials(credentials);
});
```

## Common Patterns

### Environment-Specific Configuration

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
        else if (hostEnvironment.IsStaging())
        {
            // Named profile for staging environment
            x.WithCredentials(new StoredProfileAWSCredentials("staging"));
        }
        // Production uses IAM role (no configuration)
    });

    config.Messaging(x =>
    {
        x.WithRegion(configuration.GetValue<string>("AWS:Region"));
    });
});
```

### STS Assume Role

For applications that need to assume an IAM role:

```csharp
var stsClient = new AmazonSecurityTokenServiceClient();
var assumeRoleResponse = await stsClient.AssumeRoleAsync(new AssumeRoleRequest
{
    RoleArn = "arn:aws:iam::123456789012:role/MyRole",
    RoleSessionName = "JustSayingSession"
});

var credentials = new SessionAWSCredentials(
    assumeRoleResponse.Credentials.AccessKeyId,
    assumeRoleResponse.Credentials.SecretAccessKey,
    assumeRoleResponse.Credentials.SessionToken
);

config.Client(x =>
{
    x.WithCredentials(credentials);
});
```

## Troubleshooting

### "Unable to get IAM security credentials"

This error occurs when the AWS SDK cannot find valid credentials. Check:

1. Are you running on AWS infrastructure with an IAM role attached?
2. Are environment variables `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` set?
3. Does `~/.aws/credentials` file exist with valid credentials?
4. Have you explicitly configured credentials in JustSaying?

### "The security token included in the request is expired"

Temporary credentials have expired. If using `WithSessionCredentials`, refresh the credentials from STS. If using IAM roles, ensure the EC2 instance or ECS task metadata service is accessible.

## See Also

- [Service Endpoints](service-endpoints.md) - Configure LocalStack and custom endpoints
- [AWS IAM](../aws-iam.md) - Required IAM permissions for JustSaying
- [Testing](../advanced/testing.md) - Testing strategies with LocalStack
