using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests;

/// <summary>
/// Skips the test when running against a simulator/in-memory bus or when no real AWS credentials are available.
/// </summary>
public static class NotSimulatorGuard
{
    public static void SkipIfNotSupported()
    {
        Skip.When(TestEnvironment.IsSimulatorConfigured, "This test is not supported using an AWS simulator.");
        Skip.When(!TestEnvironment.HasCredentials, "This test requires AWS credentials to be configured.");
    }
}
