using Xunit;

namespace JustSaying.IntegrationTests
{
    [CollectionDefinition(GlobalSetup.CollectionName, DisableParallelization = true)]
    public class GlobalSetupCollection : ICollectionFixture<GlobalSetup>
    {
    }
}
