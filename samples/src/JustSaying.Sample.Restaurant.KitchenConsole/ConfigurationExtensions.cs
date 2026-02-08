using Amazon;
using Microsoft.Extensions.Configuration;

namespace JustSaying.Sample.Restaurant.KitchenConsole;

public static class ConfigurationExtensions
{
    private const string AWSServiceUrlKey = "AWSServiceUrl";
    private const string LocalStackConnectionStringKey = "ConnectionStrings:localstack";
    private const string AWSRegionKey = "AWSRegion";

    public static bool HasAWSServiceUrl(this IConfiguration configuration)
    {
        return !string.IsNullOrWhiteSpace(configuration[AWSServiceUrlKey])
            || !string.IsNullOrWhiteSpace(configuration[LocalStackConnectionStringKey]);
    }

    public static Uri GetAWSServiceUri(this IConfiguration configuration)
    {
        var url = configuration[AWSServiceUrlKey]
            ?? configuration[LocalStackConnectionStringKey];
        return new Uri(url!);
    }

    public static RegionEndpoint GetAWSRegion(this IConfiguration configuration)
    {
        return RegionEndpoint.GetBySystemName(configuration[AWSRegionKey]);
    }
}
