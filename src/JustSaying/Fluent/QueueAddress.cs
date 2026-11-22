using System.Text.RegularExpressions;
using Amazon;
using Amazon.SQS;

namespace JustSaying.Fluent;

/// <summary>
/// A type that encapsulates an address of an SQS queue.
/// </summary>
internal sealed class QueueAddress
{
    private QueueAddress()
    { }

    /// <summary>
    /// The QueueUrl of the SQS queue.
    /// </summary>
    public Uri QueueUrl { get; private set; }

    /// <summary>
    /// The region of the queue.
    /// </summary>
    public string RegionName { get; private set; }

    /// <summary>
    /// Creates a <see cref="QueueAddress"/> from a queue URL.
    /// </summary>
    /// <param name="queueUrl">The queue URL.</param>
    /// <param name="regionName">Optional region name (e.g. eu-west-1), if not provided the region will be inferred from the URL, and 'unknown' if it cannot.</param>
    /// <returns>A <see cref="QueueAddress"/> created from the URL.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="queueUrl"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="queueUrl"/> is not an SQS queue URL, or <paramref name="regionName"/> contradicts the region in it.
    /// </exception>
    public static QueueAddress FromUri(Uri queueUrl, string regionName = null)
    {
        if (queueUrl == null) throw new ArgumentNullException(nameof(queueUrl));

        var pathSegments = queueUrl.Segments.Select(x => x.Trim('/')).Where(x => !string.IsNullOrEmpty(x)).ToArray();
        if (pathSegments.Length != 2)
        {
            throw new ArgumentException(
                $"'{queueUrl}' is not a valid SQS queue URL. Its path must be the account id and queue name, for example 'https://sqs.eu-west-1.amazonaws.com/123456789012/my-queue'.",
                nameof(queueUrl));
        }

        var host = queueUrl.Host;
        var hostParts = host.Split('.');

        var isAwsHost = host.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase)
                        || host.EndsWith(".amazonaws.com.cn", StringComparison.OrdinalIgnoreCase);

        // An AWS SQS endpoint is sqs.*, sqs-fips.* (FIPS), vpce-{id}.sqs.* (a VPC endpoint), or the legacy *queue.amazonaws.com.
        if (isAwsHost && !hostParts.Any(part => part.Equals("sqs", StringComparison.OrdinalIgnoreCase)
                                                || part.Equals("sqs-fips", StringComparison.OrdinalIgnoreCase)
                                                || part.Equals("queue", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"'{queueUrl}' is not an SQS queue URL.", nameof(queueUrl));
        }

        // The region is one of the host's labels wherever it sits: sqs.{region}.amazonaws.com,
        // sqs-fips.{region}.amazonaws.com, the legacy {region}.queue.amazonaws.com,
        // vpce-{id}.sqs.{region}.vpce.amazonaws.com, or LocalStack's sqs.{region}.localhost.localstack.cloud.
        // The legacy global endpoint, queue.amazonaws.com, is in us-east-1. Any other host (for example
        // LocalStack on localhost) doesn't say.
        var hostRegion = hostParts.FirstOrDefault(part => RegionPattern.IsMatch(part))?.ToLowerInvariant()
                         ?? (string.Equals(host, "queue.amazonaws.com", StringComparison.OrdinalIgnoreCase) ? RegionEndpoint.USEast1.SystemName : null);

        if (regionName is not null && hostRegion is not null && !string.Equals(regionName, hostRegion, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"The region '{regionName}' contradicts the region '{hostRegion}' in the queue URL '{queueUrl}'.",
                nameof(regionName));
        }

        var queueRegion = regionName ?? RegionEndpoint.GetBySystemName(hostRegion ?? "unknown").SystemName;
        return new QueueAddress { QueueUrl = queueUrl, RegionName = queueRegion };
    }

    // Based on https://github.com/aws/aws-sdk-net/blob/850c66f71f4ce54943700565ecea5572ce31979a/sdk/src/Core/endpoints.json#L16
    private static readonly Regex RegionPattern = new("^[a-z]{2}(-[a-z]+)+-\\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// Creates a <see cref="QueueAddress"/> from a queue URL.
    /// </summary>
    /// <param name="queueUrl">The queue URL.</param>
    /// <param name="regionName">Optional region name (e.g. eu-west-1), if not provided the region will be inferred from the URL, and 'unknown' if it cannot.</param>
    /// <returns>A <see cref="QueueAddress"/> created from the URL.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static QueueAddress FromUrl(string queueUrl, string regionName = null)
    {
        if (!Uri.TryCreate(queueUrl, UriKind.Absolute, out var queueUri)) throw new ArgumentException("Must be a valid Uri.", nameof(queueUrl));
        return FromUri(queueUri, regionName);
    }

    /// <summary>
    /// Creates a <see cref="QueueAddress"/> from a queue ARN.
    /// </summary>
    /// <param name="queueArn">The queue ARN.</param>
    /// <returns>A <see cref="QueueAddress"/> created from the ARN.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static QueueAddress FromArn(string queueArn)
    {
        if (!Arn.TryParse(queueArn, out var arn)) throw new ArgumentException("Must be a valid ARN.", nameof(queueArn));
        if (!string.Equals(arn.Service, "sqs", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Must be an ARN for an SQS queue.", nameof(queueArn));
        if (string.IsNullOrEmpty(arn.Region) || string.IsNullOrEmpty(arn.AccountId) || string.IsNullOrEmpty(arn.Resource))
        {
            throw new ArgumentException("An SQS queue ARN must include the region, the account id and the queue name.", nameof(queueArn));
        }

        var hostname = SqsEndpointHelper.GetSqsHostname(arn.Partition, arn.Region);

        var queueUrl = new UriBuilder("https", hostname)
        {
            Path = FormattableString.Invariant($"{arn.AccountId}/{arn.Resource}")
        }.Uri;

        return new QueueAddress
        {
            QueueUrl = queueUrl,
            RegionName = arn.Region
        };
    }
}
