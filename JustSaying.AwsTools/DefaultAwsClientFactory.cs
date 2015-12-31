using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace JustSaying.AwsTools
{
    public class DefaultAwsClientFactory : IAwsClientFactory
    {
        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        {
            return AWSClientFactory.CreateAmazonSimpleNotificationServiceClient(region);
        }

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
        {
            return AWSClientFactory.CreateAmazonSQSClient(region);
        }
    }
}