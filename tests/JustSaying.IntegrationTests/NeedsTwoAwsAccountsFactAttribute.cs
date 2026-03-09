using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests;

/// <summary>
/// Skips the test when secondary AWS account credentials are not available.
/// </summary>
public static class NeedsTwoAwsAccountsGuard
{
    public static void SkipIfNotSupported()
    {
        Skip.When(!TestEnvironment.HasSecondaryCredentials, "This test requires secondary AWS account credentials to be configured.");
    }
}
