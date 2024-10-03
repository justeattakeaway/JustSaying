using Xunit;

namespace JustSaying.TestingFramework;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AwsTheoryAttribute : TheoryAttribute
{
    public AwsTheoryAttribute()
        : base()
    {
    }
}
