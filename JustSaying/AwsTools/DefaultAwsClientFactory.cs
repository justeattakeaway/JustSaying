using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace JustSaying.AwsTools
{
    public class DefaultAwsClientFactory : IAwsClientFactory
    {
        private readonly AWSCredentials _credentials;
        public Uri ServiceUri { get; set; }

        public DefaultAwsClientFactory()
        {
            _credentials = FallbackCredentialsFactory.GetCredentials();
        }

        public DefaultAwsClientFactory(AWSCredentials customCredentials)
        {
            _credentials = customCredentials;
        }

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        {
            var config = new AmazonSimpleNotificationServiceConfig()
            {
                RegionEndpoint = region
            };

            if (ServiceUri != null)
            {
                config.ServiceURL = ServiceUri.ToString();
            }

            return new AmazonSimpleNotificationServiceClient(_credentials, config);
        }

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
        {
            var config = new AmazonSQSConfig()
            {
                RegionEndpoint = region
            };

            if (ServiceUri != null)
            {
                config.ServiceURL = ServiceUri.ToString();
            }

            return new AmazonSQSClient(_credentials, region);
        }


    }
}
