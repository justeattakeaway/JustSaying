using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace JustSaying.AwsTools;

public class DefaultAwsClientFactory : IAwsClientFactory
{
    private readonly AWSCredentials _credentials;

    public DefaultAwsClientFactory()
    {
        _credentials = FallbackCredentialsFactory.GetCredentials();
    }

    public DefaultAwsClientFactory(AWSCredentials customCredentials)
    {
        _credentials = customCredentials;
    }

    public Uri ServiceUri { get; set; }

    public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        => new AmazonSimpleNotificationServiceClient(_credentials, CreateSNSConfig(region));

    public IAmazonSQS GetSqsClient(RegionEndpoint region)
        => new AmazonSQSClient(_credentials, CreateSQSConfig(region));

    protected virtual void Configure(AmazonSimpleNotificationServiceConfig config)
    {
        // For derived classes to override and customise
    }

    protected virtual void Configure(AmazonSQSConfig config)
    {
        // For derived classes to override and customise
    }

    private AmazonSimpleNotificationServiceConfig CreateSNSConfig(RegionEndpoint region)
    {
        var config = new AmazonSimpleNotificationServiceConfig()
        {
            RegionEndpoint = region,
        };

        if (ServiceUri != null)
        {
            config.ServiceURL = ServiceUri.ToString();
        }

        Configure(config);

        return config;
    }

    private AmazonSQSConfig CreateSQSConfig(RegionEndpoint region)
    {
        var config = new AmazonSQSConfig()
        {
            RegionEndpoint = region,
        };

        if (ServiceUri != null)
        {
            config.ServiceURL = ServiceUri.ToString();
        }

        Configure(config);

        return config;
    }
}