using System;
using JustSaying.TestingFramework;
using Xunit;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsTopicSubscriber
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class NeedsTwoAwsAccountsFactAttribute : FactAttribute
    {
        public NeedsTwoAwsAccountsFactAttribute()
            : base()
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
}
