using System.Web;
using JustEat.Simples.Common.Services;
using JustEat.Testing;

namespace JustEat.Simples.Common.UnitTests.Services.Cache
{
    public class WhenSettingTimeoutOnInProcCacheBase : BehaviourTest<InProcCacheService> 
    {
        protected string Key;
        protected CacheableClass Expected;
        protected int Timeout;
        protected CacheableClass Result;

        protected override void Given()
        {
            Key = "__CacheableClass";
            Expected = new CacheableClass { Value = "TestValue" };
            Timeout = 30;
        }

        protected override InProcCacheService CreateSystemUnderTest()
        {
            var cacheManager = new InProcCacheService(HttpRuntime.Cache);
            cacheManager.InsertWithTimeout(Key, Expected, Timeout);
            return cacheManager;
        }

        protected override void When()
        {
            
        }

        protected class CacheableClass
        {
            public string Value { get; set; }
        }
    }
}