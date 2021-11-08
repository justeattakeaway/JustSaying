using Xunit;

namespace JustSaying.TestingFramework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AwsTheoryAttribute : TheoryAttribute
    {
        public AwsTheoryAttribute()
            : base()
        {
            if (!TestEnvironment.IsSimulatorConfigured && !TestEnvironment.HasCredentials)
            {
                Skip = "This test requires either an AWS simulator URL or AWS credentials to be configured.";
            }
        }
    }
}
