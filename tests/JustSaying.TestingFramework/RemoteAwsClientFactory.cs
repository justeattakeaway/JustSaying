using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools;

namespace JustSaying.TestingFramework;

public class RemoteAwsClientFactory : IAwsClientFactory
{
    public RemoteAwsClientFactory()
    {
    }

    /// <inheritdoc />
    public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        => new AmazonSimpleNotificationServiceClient(CredentialsFromEnvironment(), region);

    /// <inheritdoc />
    public IAmazonSQS GetSqsClient(RegionEndpoint region)
        => new AmazonSQSClient(CredentialsFromEnvironment(), region);

    private static AWSCredentials CredentialsFromEnvironment()
        => TestEnvironment.Credentials;
}