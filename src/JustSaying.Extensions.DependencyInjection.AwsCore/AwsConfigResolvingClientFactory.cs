using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.Extensions.DependencyInjection.AwsCore;

internal class AwsConfigResolvingClientFactory : IAwsClientFactory
{

    private readonly IServiceProvider _serviceProvider;
    public AwsConfigResolvingClientFactory(IServiceProvider provider)
    {
        _serviceProvider = provider;
    }

    public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
    {
        var sns = _serviceProvider.GetRequiredService<IAmazonSimpleNotificationService>();
        if(string.IsNullOrEmpty(sns.Config.ServiceURL) && sns.Config.RegionEndpoint != region)
        {
            throw new ArgumentException($"Configured region {sns.Config.RegionEndpoint.DisplayName} does not match the region argument {region.DisplayName}");
        }
        return sns;
    }

    public IAmazonSQS GetSqsClient(RegionEndpoint region)
    {
        var sqs = _serviceProvider.GetRequiredService<IAmazonSQS>();
        if (string.IsNullOrEmpty(sqs.Config.ServiceURL) && sqs.Config.RegionEndpoint != region)
        {
            throw new ArgumentException($"Configured region {sqs.Config.RegionEndpoint.DisplayName} does not match the region argument {region.DisplayName}");
        }
        return sqs;
    }

}
