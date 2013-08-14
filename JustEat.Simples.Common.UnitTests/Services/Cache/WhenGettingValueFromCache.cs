using JustEat.Testing;
using NUnit.Framework;

namespace JustEat.Simples.Common.UnitTests.Services.Cache
{
    public class WhenGettingValueFromInProcCache : WhenSettingTimeoutOnInProcCacheBase
    {
        protected override void When()
        {
            Result = SystemUnderTest.Get<CacheableClass>(Key);
        }

        [Then]
        public void ResultContainsExpectedValue()
        {
            Assert.AreSame(Expected, Result);
        }
    }
}
