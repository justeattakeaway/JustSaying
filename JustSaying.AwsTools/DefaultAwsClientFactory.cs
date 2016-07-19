using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace JustSaying.AwsTools
{
    public class DefaultAwsClientFactory : IAwsClientFactory
    {
        private readonly AWSCredentials _credentials;

        public DefaultAwsClientFactory()
        {
            _credentials = new StoredProfileAWSCredentials("default");
        }

        public DefaultAwsClientFactory(AWSCredentials customCredentials)
        {
            _credentials = customCredentials;
        }

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region) 
            => new AmazonSimpleNotificationServiceClient(_credentials, region);

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
            => new AmazonSQSClient(_credentials, region);
    }
}