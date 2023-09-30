using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools;

namespace JustSaying.TestingFramework;

public class LocalAwsClientFactory(Uri serviceUrl) : IAwsClientFactory
{
    private Uri ServiceUrl { get; } = serviceUrl;

    public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
    {
        var credentials = new AnonymousAWSCredentials();
        var clientConfig = new AmazonSimpleNotificationServiceConfig
        {
            RegionEndpoint = region,
            ServiceURL = ServiceUrl.ToString()
        };

        return new AmazonSimpleNotificationServiceClient(credentials, clientConfig);
    }

    public IAmazonSQS GetSqsClient(RegionEndpoint region)
    {
        var credentials = new AnonymousAWSCredentials();
        var clientConfig = new AmazonSQSConfig
        {
            RegionEndpoint = region,
            ServiceURL = ServiceUrl.ToString()
        };

        return new AmazonSQSClient(credentials, clientConfig);
    }
}