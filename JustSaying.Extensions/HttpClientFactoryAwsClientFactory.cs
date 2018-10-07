using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools;

namespace JustSaying.Extensions
{
    public sealed class HttpClientFactoryAwsClientFactory : IAwsClientFactory
    {
        public HttpClientFactoryAwsClientFactory()
            : this(FallbackCredentialsFactory.GetCredentials())
        {
        }

        public HttpClientFactoryAwsClientFactory(AWSCredentials credentials)
        {
            Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        }

        private AWSCredentials Credentials { get; }

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
            => new CustomAmazonSimpleNotificationServiceClient(Credentials, region);

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
            => new CustomAmazonSQSClient(Credentials, region);
    }
}
