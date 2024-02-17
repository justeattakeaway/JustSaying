using System.Text.RegularExpressions;
using Amazon;

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
    /// <exception cref="ArgumentException"></exception>
    public static QueueAddress FromUri(Uri queueUrl, string regionName = null)
    {
        var queueRegion = regionName ?? ParseRegionFromUri(queueUrl);
        return new QueueAddress { QueueUrl = queueUrl, RegionName = queueRegion };

        static string ParseRegionFromUri(Uri queueUri)
        {
            string regionName = "unknown";
            var hostParts = queueUri.Host.Split('.');
            // AWS cloud region endpoints are of the form "{service}.{region}.{dnsSuffix}" - https://github.com/aws/aws-sdk-net/blob/850c66f71f4ce54943700565ecea5572ce31979a/sdk/src/Core/endpoints.json#L5
            if (hostParts.Length >= 3)
            {
                var servicePart = hostParts[0];
                if (!string.Equals(servicePart, "sqs", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Must be an ARN for an SQS queue.", nameof(queueUri));

                var regionHostPart = hostParts[1];
                // Based on this: https://github.com/aws/aws-sdk-net/blob/850c66f71f4ce54943700565ecea5572ce31979a/sdk/src/Core/endpoints.json#L16
                if (Regex.IsMatch(regionHostPart, "^[a-z]{2}\\-\\w+\\-\\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase))
                {
                    regionName = regionHostPart;
                }
            }

            return RegionEndpoint.GetBySystemName(regionName).SystemName;
        }
    }

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

#pragma warning disable CS0618 // Type or member is obsolete
        var hostname = RegionEndpoint.GetBySystemName(arn.Region)
            .GetEndpointForService("sqs")
            .Hostname;
#pragma warning restore CS0618 // Type or member is obsolete

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
