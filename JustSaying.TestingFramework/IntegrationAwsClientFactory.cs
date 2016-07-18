using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustSaying.AwsTools;

namespace JustSaying.TestingFramework
{
    /// <summary>
    /// AwsCustomClient factory for running integration tests in continuous integration mode.
    /// 
    /// If environment variable "ci" is not defined, use default AWS profile name as specified by <code>IntegrationTestConfig.AwsProfileName</code>
    /// 
    /// Otherwise, construct AWS Profile using access and secret key by looking at "ci-access_key" and "ci-secret_key" environment variables.
    /// </summary>
    public class IntegrationAwsClientFactory : IAwsClientFactory
    {
        private readonly AWSCredentials _credentials;
        private readonly string _ci = "ci";
        private readonly string _ciAccesskey = "AWS_ACCESS_KEY_ID";
        private readonly string _ciSecretkey = "AWS_SECRET_KEY";

        public IntegrationAwsClientFactory()
        {
            var ci = System.Environment.GetEnvironmentVariable(_ci);
            if (string.IsNullOrWhiteSpace(ci))
            {
                _credentials = new StoredProfileAWSCredentials(IntegrationTestConfig.AwsProfileName);
            }
            else
            {
                _credentials = CredentialsFromEnvironment();
            }
        }

        private AWSCredentials CredentialsFromEnvironment()
        {
            var accessKey = System.Environment.GetEnvironmentVariable(_ciAccesskey);
            var secretKey = System.Environment.GetEnvironmentVariable(_ciSecretkey);
            return new BasicAWSCredentials(accessKey, secretKey);
        }

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        {
            return new AmazonSimpleNotificationServiceClient(_credentials, region);
        }

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
        {
            return new AmazonSQSClient(_credentials, region);
        }
    }
}