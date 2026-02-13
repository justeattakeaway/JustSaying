using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class NotSimulatorFactAttribute : FactAttribute
{
    public NotSimulatorFactAttribute(
        [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        : base(sourceFilePath, lineNumber)
    {
        if (TestEnvironment.IsSimulatorConfigured)
        {
            Skip = "This test is not supported using an AWS simulator.";
        }
        else if (!TestEnvironment.HasCredentials)
        {
            Skip = "This test requires AWS credentials to be configured.";
        }
    }
}