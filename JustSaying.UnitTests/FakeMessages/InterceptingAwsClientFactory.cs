using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using JustEat.HttpClientInterception;
using JustSaying.AwsTools;

namespace JustSaying.UnitTests.FakeMessages
{
    internal class InterceptingAwsClientFactory : IAwsClientFactory
    {
        public InterceptingAwsClientFactory(AWSCredentials credentials, HttpClientInterceptorOptions options)
        {
            Credentials = credentials;
            Options = options;
        }

        private AWSCredentials Credentials { get; }

        private HttpClientInterceptorOptions Options { get; }

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        {
            throw new NotImplementedException();
        }

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
        {
            var config = new AmazonSQSConfig()
            {
                RegionEndpoint = region,
                HttpClientFactory = new InterceptingHttpClientFactory(Options),
            };

            return new AmazonSQSClient(Credentials, config);
        }
    }
}
