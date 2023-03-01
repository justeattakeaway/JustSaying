using Amazon;
using Microsoft.Extensions.Configuration;

namespace JustSaying.Sample.Middleware.Extensions;

internal static class ConfigurationExtensions
{
    private const string AWSServiceUrlKey = "AWSServiceUrl";

    private const string AWSRegionKey = "AWSRegion";

    internal static bool HasAWSServiceUrl(this IConfiguration configuration) => !string.IsNullOrWhiteSpace(configuration[AWSServiceUrlKey]);

    internal static Uri GetAWSServiceUri(this IConfiguration configuration) => new(configuration[AWSServiceUrlKey]);

    internal static RegionEndpoint GetAWSRegion(this IConfiguration configuration) => RegionEndpoint.GetBySystemName(configuration[AWSRegionKey]);
}
