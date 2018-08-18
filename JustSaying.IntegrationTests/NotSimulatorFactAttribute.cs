using System;
using JustSaying.TestingFramework;
using Xunit;

namespace JustSaying.IntegrationTests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class NotSimulatorFactAttribute : FactAttribute
    {
        public NotSimulatorFactAttribute()
            : base()
        {
            if (TestEnvironment.IsSimulatorConfigured)
            {
                Skip = "This test is not supported using an AWS simulator.";
            }
        }
    }
}
