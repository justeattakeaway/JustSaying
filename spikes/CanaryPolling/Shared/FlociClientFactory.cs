using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools;

namespace CanaryDemo.Shared;

/// <summary>
/// Real AWS SDK clients pointed at a floci container. Floci treats a 12-digit
/// access key id as the account id, giving each demo run isolated resources.
/// </summary>
public sealed class FlociClientFactory(Uri serviceUrl, string accountId, string region) : IAwsClientFactory
{
    private readonly AWSCredentials _credentials = new BasicAWSCredentials(accountId, "secret");

    public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint regionEndpoint) =>
        new AmazonSimpleNotificationServiceClient(_credentials, new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = serviceUrl.ToString(),
            AuthenticationRegion = ResolveRegion(regionEndpoint),
        });

    public IAmazonSQS GetSqsClient(RegionEndpoint regionEndpoint) =>
        new AmazonSQSClient(_credentials, new AmazonSQSConfig
        {
            ServiceURL = serviceUrl.ToString(),
            AuthenticationRegion = ResolveRegion(regionEndpoint),
        });

    // JustSaying falls back to "unknown" when it cannot parse a region from a custom
    // queue URL; pin those calls back to the demo's region so they hit the same
    // floci partition.
    private string ResolveRegion(RegionEndpoint regionEndpoint)
    {
        var name = regionEndpoint?.SystemName;
        return string.IsNullOrEmpty(name) || string.Equals(name, "unknown", StringComparison.OrdinalIgnoreCase)
            ? region
            : name;
    }
}
