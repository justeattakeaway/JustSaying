using Xunit;

namespace JustSaying.TestingFramework;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AwsFactAttribute : FactAttribute
{
    public AwsFactAttribute()
        : base()
    {
        if (!TestEnvironment.IsSimulatorConfigured && !TestEnvironment.HasCredentials)
        {
            Skip = "This test requires either an AWS simulator URL or AWS credentials to be configured.";
        }
    }
}