using System;
using JustSaying.TestingFramework;
using Xunit;

namespace JustSaying.IntegrationTests
{
    public class GlobalSetup : IDisposable
    {
        public const string CollectionName = "Global Fixture Setup";

        public GlobalSetup()
        {
            CreateMeABus.DefaultClientFactory = () => new IntegrationAwsClientFactory();
        }

        public void Dispose()
        {
        }
    }

    [CollectionDefinition(GlobalSetup.CollectionName)]
    public class GlobalSetupCollection : ICollectionFixture<GlobalSetup>
    {
        
    }
}
