using System;
using Xunit;

namespace JustSaying.TestingFramework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AwsFactAttribute : FactAttribute
    {
        public AwsFactAttribute()
            : base()
        {

        }
    }
}
