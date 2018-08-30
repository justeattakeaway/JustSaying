using System;
using JustSaying.AwsTools;
using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests
{
    public sealed class GlobalSetup : IDisposable
    {
        public const string CollectionName = "Global Fixture Setup";

        public GlobalSetup()
        {
            IAwsClientFactory clientFactory;

            if (TestEnvironment.IsSimulatorConfigured)
            {
                clientFactory = new LocalAwsClientFactory(TestEnvironment.SimulatorUrl);
            }
            else
            {
                clientFactory = new RemoteAwsClientFactory();
            }

            CreateMeABus.DefaultClientFactory = () => clientFactory;
        }

        public void Dispose()
        {
            CreateMeABus.DefaultClientFactory = () => new DefaultAwsClientFactory();
        }
    }
}
