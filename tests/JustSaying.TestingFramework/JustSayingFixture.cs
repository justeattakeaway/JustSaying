using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace JustSaying.TestingFramework
{
    public class JustSayingFixture
    {
        public JustSayingFixture()
        {
        }

        public JustSayingFixture(ITestOutputHelper outputHelper)
        {
            LoggerFactory = outputHelper.ToLoggerFactory();
        }

        public AWSCredentials Credentials { get; set; } = TestEnvironment.Credentials;

        public static bool IsSimulator => TestEnvironment.IsSimulatorConfigured;

        public ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

        public RegionEndpoint Region { get; set; } = TestEnvironment.Region;

        public string RegionName => Region?.SystemName;

        public string UniqueName { get; set; } = $"queuename-{DateTime.Now.Ticks}";

        public IMayWantOptionalSettings Builder()
            => CreateMeABus.WithLogging(LoggerFactory).InRegion(RegionName);

        public IAmazonSimpleNotificationService CreateSnsClient()
            => CreateMeABus.DefaultClientFactory().GetSnsClient(Region);

        public IAmazonSQS CreateSqsClient()
            => CreateMeABus.DefaultClientFactory().GetSqsClient(Region);
    }
}
