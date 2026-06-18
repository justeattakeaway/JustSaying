using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools;

namespace JustSaying.TestingFramework;

/// <summary>
/// An <see cref="IAwsClientFactory"/> that points real AWS SDK clients at a
/// running floci (https://github.com/floci-io/floci) instance.
/// </summary>
/// <remarks>
/// Floci interprets a 12-digit access key id as an AWS account id, which gives
/// each test its own isolated set of resources without coordination, so the same
/// concurrency story as the in-memory <c>LocalSqsSnsMessaging</c> bus applies.
/// </remarks>
public sealed class FlociAwsClientFactory : IAwsClientFactory
{
    private readonly Uri _serviceUrl;
    private readonly string _fallbackRegion;
    private readonly AWSCredentials _credentials;

    public FlociAwsClientFactory(Uri serviceUrl, string accountId, string fallbackRegion)
    {
        ArgumentNullException.ThrowIfNull(serviceUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackRegion);

        _serviceUrl = serviceUrl;
        _fallbackRegion = fallbackRegion;
        _credentials = new BasicAWSCredentials(accountId, "secret");
    }

    public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
    {
        var config = new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = _serviceUrl.ToString(),
            AuthenticationRegion = ResolveRegion(region),
        };

        return new AmazonSimpleNotificationServiceClient(_credentials, config);
    }

    public IAmazonSQS GetSqsClient(RegionEndpoint region)
    {
        var config = new AmazonSQSConfig
        {
            ServiceURL = _serviceUrl.ToString(),
            AuthenticationRegion = ResolveRegion(region),
        };

        return new AmazonSQSClient(_credentials, config);
    }

    // Floci scopes resources by the region in the credential scope. JustSaying
    // falls back to "unknown" when it cannot parse a region from a custom queue
    // URL (e.g. http://localhost:4567/{account}/{queue}), which would route to
    // a different region partition in floci. Pin those calls to the fallback
    // region so they reach the same resources the rest of the test used.
    private string ResolveRegion(RegionEndpoint region)
    {
        var name = region?.SystemName;
        return string.IsNullOrEmpty(name) || string.Equals(name, "unknown", StringComparison.OrdinalIgnoreCase)
            ? _fallbackRegion
            : name;
    }
}
