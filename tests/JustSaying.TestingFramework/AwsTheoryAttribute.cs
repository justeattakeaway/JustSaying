using Xunit;

namespace JustSaying.TestingFramework;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AwsTheoryAttribute : TheoryAttribute
{
    public AwsTheoryAttribute()
        : base()
    {
        // TODO Add back logic to check if AWS credentials are available when running with LocalStack
        // at the moment we are not using LocalStack so we can skip this check
    }
}
