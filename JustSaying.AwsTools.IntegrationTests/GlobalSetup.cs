using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [SetUp]
        public void SetUp()
        {
            CreateMeABus.DefaultClientFactory = () => new IntegrationAwsClientFactory();
        }
    }
}