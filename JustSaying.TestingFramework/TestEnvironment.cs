using System;
using Amazon;
using Amazon.Runtime;

namespace JustSaying.TestingFramework
{
    /// <summary>
    /// A class containing information about the integration test environment. This class cannot be inherited.
    /// </summary>
    public static class TestEnvironment
    {
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
        public static bool IsSimulatorConfigured => !string.IsNullOrEmpty(SimulatorUrl);

        /// <summary>
        /// Gets the URL for the configured AWS simulator, if any.
        /// </summary>
        public static string SimulatorUrl => "http://localhost:4100" ?? Environment.GetEnvironmentVariable("AWS_SERVICE_URL") ?? string.Empty;

        /// <summary>
        /// Gets a value indicating whether AWS credentials are configured.
        /// </summary>
        public static bool HasCredentials => !string.IsNullOrEmpty(AccessKey) && !string.IsNullOrEmpty(SecretKey);

        /// <summary>
        /// Gets the configured AWS credentials, if any.
        /// </summary>
        public static AWSCredentials Credentials => HasCredentials ? null : new BasicAWSCredentials(AccessKey, SecretKey);

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
}
