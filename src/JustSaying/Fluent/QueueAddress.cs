using System;
using Amazon;

namespace JustSaying.Fluent
{
    public sealed class QueueAddress
    {
        internal QueueAddress()
        { }

        public Uri QueueUrl { get; internal set; }
        public string RegionName { get; internal set; }

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
                    var regionHostPart = hostParts[1];
                    if (servicePart == "sqs" && RegionEndpoint.GetBySystemName(regionHostPart) is { } regionEndpoint )
                    {
                        return regionEndpoint.SystemName;
                    }
                }
                throw new InvalidOperationException("Could not infer region from queueUri, please specify the region using the regionName argument. If you are using localstack, the default region is us-east-1.");
            }
        }

        public static QueueAddress FromArn(string queueArn)
        {
            if (!Arn.TryParse(queueArn, out var arn)) throw new ArgumentException("Must be a valid ARN.", nameof(queueArn));
            var dnsSuffix = RegionEndpoint.GetBySystemName(arn.Region).PartitionDnsSuffix;
            return new QueueAddress
            {
                QueueUrl = new Uri($"https://{arn.Service}.{arn.Region}.{dnsSuffix}/{arn.AccountId}/{arn.Resource}"),
                RegionName = arn.Region
            };
        }
    }
}
