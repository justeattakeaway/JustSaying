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
        // With no explicit credentials the clients resolve the default credential chain when the
        // first request is made, so creating clients (and building the bus) works on machines
        // with no resolvable credentials, such as generating an AsyncAPI document on CI.
    }

    public DefaultAwsClientFactory(AWSCredentials customCredentials)
    {
        _credentials = customCredentials;
    }

    public Uri ServiceUri { get; set; }

    public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        => _credentials is null
            ? new AmazonSimpleNotificationServiceClient(CreateSNSConfig(region))
            : new AmazonSimpleNotificationServiceClient(_credentials, CreateSNSConfig(region));

    public IAmazonSQS GetSqsClient(RegionEndpoint region)
        => _credentials is null
            ? new AmazonSQSClient(CreateSQSConfig(region))
            : new AmazonSQSClient(_credentials, CreateSQSConfig(region));

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
