using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools;

namespace JustSaying.TestingFramework
{
    public class LocalAwsClientFactory : IAwsClientFactory
    {
        public LocalAwsClientFactory(string serviceUrl)
        {
            ServiceUrl = serviceUrl;
        }

        private string ServiceUrl { get; }

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        {
            var credentials = new AnonymousAWSCredentials();
            var clientConfig = new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = ServiceUrl
            };

            return new AmazonSimpleNotificationServiceClient(credentials, clientConfig);
        }

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
        {
            var credentials = new AnonymousAWSCredentials();
            var clientConfig = new AmazonSQSConfig
            {
                ServiceURL = ServiceUrl
            };

            return new AmazonSQSClient(credentials, clientConfig);
        }
    }
}
