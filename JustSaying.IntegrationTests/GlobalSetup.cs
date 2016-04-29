using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.IntegrationTests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public static void SetUp()
        {
            CreateMeABus.DefaultClientFactory = () => new IntegrationAwsClientFactory();
        }
    }
}