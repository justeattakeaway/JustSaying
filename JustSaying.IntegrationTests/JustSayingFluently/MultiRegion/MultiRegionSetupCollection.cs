using Xunit;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion
{
    [CollectionDefinition(MultiRegionSetup.CollectionName, DisableParallelization = true)]
    public class MultiRegionSetupCollection : ICollectionFixture<MultiRegionSetup>
    {
    }
}
