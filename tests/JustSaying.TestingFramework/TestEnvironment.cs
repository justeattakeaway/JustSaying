using Amazon;
using Amazon.Runtime;

namespace JustSaying.TestingFramework;

/// <summary>
/// A class containing information about the integration test environment. This class cannot be inherited.
/// </summary>
public static class TestEnvironment
{
    /// <summary>
    /// Gets the configured AWS account ID, if any.
    /// </summary>
    public static string AccountId => Environment.GetEnvironmentVariable("AWS_ACCOUNT_ID");

    /// <summary>
    /// Gets the AWS region configured for use.
    /// </summary>
    public static RegionEndpoint Region
    {
        get
        {
            string systemName = RegionName;

            if (string.IsNullOrEmpty(systemName))
            {
                return RegionEndpoint.EUWest1;
            }
            else
            {
                return RegionEndpoint.GetBySystemName(systemName);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an AWS simulator is configured for use.
    /// </summary>
    public static bool IsSimulatorConfigured => SimulatorUrl != null;

    /// <summary>
    /// Gets the URL for the configured AWS simulator, if any.
    /// </summary>
    public static Uri SimulatorUrl
    {
        get
        {
            var awsEnv = Environment.GetEnvironmentVariable("AWS_SERVICE_URL");
            if (string.IsNullOrWhiteSpace(awsEnv))
            {
                return null;
            }

            return new Uri(awsEnv, UriKind.Absolute);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the integration tests should run against the floci
    /// local AWS emulator (started in a container via Aspire) instead of the in-memory
    /// <c>LocalSqsSnsMessaging</c> bus.
    /// </summary>
    /// <remarks>
    /// Enabled by setting the <c>USE_FLOCI</c> environment variable to <c>1</c>. When enabled,
    /// the tests use real AWS SDK clients pointed at the floci container. Floci reads the access
    /// key id as an account id when it is exactly 12 digits, which lets us give each test its own
    /// account for isolation when running concurrently. The container endpoint is supplied at
    /// runtime by the Aspire test host, not via an environment variable.
    /// </remarks>
    public static bool UseFloci =>
        string.Equals(Environment.GetEnvironmentVariable("USE_FLOCI"), "1", StringComparison.Ordinal);

    /// <summary>
    /// Gets a value indicating whether AWS credentials are configured.
    /// </summary>
    public static bool HasCredentials => !string.IsNullOrEmpty(AccessKey) && !string.IsNullOrEmpty(SecretKey);

    /// <summary>
    /// Gets the configured AWS credentials, if any.
    /// </summary>
    public static AWSCredentials Credentials => HasCredentials ? null : new BasicAWSCredentials(AccessKey, SecretKey);

    /// <summary>
    /// Gets the configured secondary AWS account ID, if any.
    /// </summary>
    public static string SecondaryAccountId => Environment.GetEnvironmentVariable("AWS_ACCOUNT_ID_SECONDARY");

    /// <summary>
    /// Gets the secondary AWS region configured for use.
    /// </summary>
    public static RegionEndpoint SecondaryRegion
    {
        get
        {
            string systemName = SecondaryRegionName;

            if (string.IsNullOrEmpty(systemName))
            {
                return RegionEndpoint.USEast1;
            }
            else
            {
                return RegionEndpoint.GetBySystemName(systemName);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether secondary AWS credentials are configured.
    /// </summary>
    public static bool HasSecondaryCredentials => !string.IsNullOrEmpty(SecondaryAccessKey) && !string.IsNullOrEmpty(SecondarySecretKey);

    /// <summary>
    /// Gets the configured secondary AWS credentials, if any.
    /// </summary>
    public static AWSCredentials SecondaryCredentials => HasCredentials ? null : new BasicAWSCredentials(SecondaryAccessKey, SecondarySecretKey);

    private static string AccessKey => Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");

    private static string SecretKey => Environment.GetEnvironmentVariable("AWS_SECRET_KEY");

    private static string SecondaryAccessKey => Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_SECONDARY");

    private static string SecondarySecretKey => Environment.GetEnvironmentVariable("AWS_SECRET_KEY_SECONDARY");

    private static string RegionName => Environment.GetEnvironmentVariable("AWS_REGION");

    private static string SecondaryRegionName => Environment.GetEnvironmentVariable("AWS_REGION_SECONDARY");
}
