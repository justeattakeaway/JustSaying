using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace JustSaying.AwsTools
{
    public interface IAwsClientFactory
    {
        IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region);
        IAmazonSQS GetSqsClient(RegionEndpoint region);
    }
}