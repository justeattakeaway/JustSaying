using JustSaying.IntegrationTests.Aspire;
using JustSaying.TestingFramework;
using TUnit.Core.Interfaces;

// When running against floci, all the integration tests share a single real container.
// At full parallelism ~90 tests, each running its own polling subscription bus, overwhelm
// it and timing-sensitive assertions flake. Cap concurrency in floci mode; leave the
// in-memory bus (the default) effectively unrestricted.
[assembly: ParallelLimiter<FlociParallelLimit>]

namespace JustSaying.IntegrationTests.Aspire;

public sealed class FlociParallelLimit : IParallelLimit
{
    public int Limit => TestEnvironment.UseFloci ? 4 : int.MaxValue;
}
