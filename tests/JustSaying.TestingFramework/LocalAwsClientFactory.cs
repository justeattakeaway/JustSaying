using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools;
using LocalSqsSnsMessaging;

namespace JustSaying.TestingFramework;

public sealed class LocalAwsClientFactory : IAwsClientFactory
{
    private readonly InMemoryAwsBus _bus;

    public LocalAwsClientFactory(InMemoryAwsBus bus)
    {
        _bus = bus;
    }
    public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
    {
        return _bus.CreateSnsClient();
    }

    public IAmazonSQS GetSqsClient(RegionEndpoint region)
    {
        return _bus.CreateSqsClient();
    }
}
