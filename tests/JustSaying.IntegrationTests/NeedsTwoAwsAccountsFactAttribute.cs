using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class NeedsTwoAwsAccountsFactAttribute : FactAttribute
{
    public NeedsTwoAwsAccountsFactAttribute(
        [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        : base(sourceFilePath, lineNumber)
    {
        if (string.IsNullOrEmpty(TestEnvironment.AccountId) ||
            string.IsNullOrEmpty(TestEnvironment.SecondaryAccountId) ||
            !TestEnvironment.HasCredentials ||
            !TestEnvironment.HasSecondaryCredentials)
        {
            Skip = "Requires IDs and credentials for two AWS accounts.";
        }
    }
}