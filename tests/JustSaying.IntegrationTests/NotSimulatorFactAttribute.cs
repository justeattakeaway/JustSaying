using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests;

/// <summary>
/// Skips the test when running against a simulator/in-memory bus or when no real AWS credentials are available.
/// </summary>
public sealed class NotSimulatorSkipAttribute()
    : SkipAttribute(TestEnvironment.IsSimulatorConfigured
        ? "This test is not supported using an AWS simulator."
        : "This test requires AWS credentials to be configured.")
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
        => Task.FromResult(TestEnvironment.IsSimulatorConfigured || !TestEnvironment.HasCredentials);
}
