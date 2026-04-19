using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests;

/// <summary>
/// Skips the test when secondary AWS account credentials are not available.
/// </summary>
public sealed class NeedsTwoAwsAccountsSkipAttribute()
    : SkipAttribute("This test requires secondary AWS account credentials to be configured.")
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
        => Task.FromResult(!TestEnvironment.HasSecondaryCredentials);
}
