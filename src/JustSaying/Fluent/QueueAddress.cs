using System;
using Amazon;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A type that encapsulates an address of an SQS queue.
    /// </summary>
    public sealed class QueueAddress
    {
        internal QueueAddress()
        { }

        private QueueAddress(bool isNone)
        {
            _isNone = isNone;
        }

        /// <summary>
        /// The QueueUrl of the SQS queue.
        /// </summary>
        public Uri QueueUrl { get; internal set; }

        /// <summary>
        /// The region of the queue.
        /// </summary>
        public string RegionName { get; internal set; }

        private readonly bool _isNone = false;

        /// <summary>
        /// Use <see cref="None"/> to have JustSaying automatically create your queue.
        /// </summary>
        public static QueueAddress None { get; } = new(true);

        /// <summary>
        /// Creates a <see cref="QueueAddress"/> from a queue URL.
        /// </summary>
        /// <param name="queueUrl">The queue URL.</param>
        /// <param name="regionName">Optional region name (e.g. eu-west-1), this can be omitted if the region can be inferred from the URL.</param>
        /// <returns>A <see cref="QueueAddress"/> created from the URL.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static QueueAddress FromUrl(Uri queueUrl, string regionName = null)
        {
            var queueRegion = regionName ?? ParseRegionFromUri(queueUrl);
            return new QueueAddress { QueueUrl = queueUrl, RegionName = queueRegion };

            static string ParseRegionFromUri(Uri queueUri)
            {
                var hostParts = queueUri.Host.Split('.');
                if (hostParts.Length >= 2)
                {
                    var servicePart = hostParts[0];
                    if (!string.Equals(servicePart, "sqs", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Must be an ARN for an SQS queue.");

                    var regionHostPart = hostParts[1];
                    if (RegionEndpoint.GetBySystemName(regionHostPart) is { } regionEndpoint )
                    {
                        return regionEndpoint.SystemName;
                    }
                }
                throw new ArgumentException("Could not infer region from queueUri, please specify the region using the regionName argument. If you are using localstack, the default region is us-east-1.");
            }
        }

        /// <summary>
        /// Creates a <see cref="QueueAddress"/> from a queue URL.
        /// </summary>
        /// <param name="queueUrl">The queue URL.</param>
        /// <param name="regionName">Optional region name (e.g. eu-west-1), this can be omitted if the region can be inferred from the URL.</param>
        /// <returns>A <see cref="QueueAddress"/> created from the URL.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static QueueAddress FromUrl(string queueUrl, string regionName = null)
        {
            if (!Uri.TryCreate(queueUrl, UriKind.Absolute, out var queueUri)) throw new ArgumentException("Must be a valid Uri.", nameof(queueUri));
            return FromUrl(queueUri, regionName);
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
            if (!string.Equals(arn.Service, "sqs", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Must be an ARN for an SQS queue.");

            var hostname = RegionEndpoint.GetBySystemName(arn.Region)
                .GetEndpointForService("sqs")
                .Hostname;

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
}
