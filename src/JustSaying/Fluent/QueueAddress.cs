using System;
using System.Globalization;
using Amazon;

namespace JustSaying.Fluent
{
    public sealed class QueueAddress
    {
        internal QueueAddress()
        { }

        private QueueAddress(bool isNone)
        {
            _isNone = isNone;
        }

        public Uri QueueUrl { get; internal set; }
        public string RegionName { get; internal set; }

        private readonly bool _isNone = false;

        /// <summary>
        /// Use <see cref="None"/> to have JustSaying automatically create your queue.
        /// </summary>
        public static QueueAddress None { get; } = new(true);

        public static QueueAddress FromUrl(string queueUrl, string regionName = null)
        {
            if (!Uri.TryCreate(queueUrl, UriKind.Absolute, out var queueUri)) throw new ArgumentException("Must be a valid Uri.", nameof(queueUri));

            var queueRegion = regionName ?? ParseRegionFromUri(queueUri);
            return new QueueAddress { QueueUrl = queueUri, RegionName = queueRegion };

            static string ParseRegionFromUri(Uri queueUri)
            {
                var hostParts = queueUri.Host.Split('.');
                if (hostParts.Length >= 2)
                {
                    var servicePart = hostParts[0];
                    if (servicePart != "sqs") throw new ArgumentException("Must be an ARN for an SQS queue.");

                    var regionHostPart = hostParts[1];
                    if (RegionEndpoint.GetBySystemName(regionHostPart) is { } regionEndpoint )
                    {
                        return regionEndpoint.SystemName;
                    }
                }
                throw new ArgumentException("Could not infer region from queueUri, please specify the region using the regionName argument. If you are using localstack, the default region is us-east-1.");
            }
        }

        public static QueueAddress FromArn(string queueArn)
        {
            if (!Arn.TryParse(queueArn, out var arn)) throw new ArgumentException("Must be a valid ARN.", nameof(queueArn));
            if (arn.Service != "sqs") throw new ArgumentException("Must be an ARN for an SQS queue.");
            var endpoint = RegionEndpoint.GetBySystemName(arn.Region).GetEndpointForService("sqs", false);
            var queueUrl = new Uri(FormattableString.Invariant($"https://{endpoint.Hostname}/{arn.AccountId}/{arn.Resource}"));

            return new QueueAddress
            {
                QueueUrl = queueUrl,
                RegionName = arn.Region
            };
        }
    }
}
