using System.Collections.Generic;
using System.Web;
using JustEat.Simples.Common.Services;
using JustEat.Testing;
using NUnit.Framework;

namespace JustEat.Simples.Common.UnitTests.Services.Cache
{
    public class WhenGettingValueFromInProcCacheForNullKey : BehaviourTest<InProcCacheService> 
    {
        protected string Key;
        private IDictionary<string, string> _result;

        protected override void Given()
        {
            Key = "WhenGettingValueFromInProcCacheForNullKey";
        }

        protected override InProcCacheService CreateSystemUnderTest()
        {
            HttpRuntime.Cache.Remove(Key);
            var cacheManager = new InProcCacheService(HttpRuntime.Cache);
            return cacheManager;
        }

        protected override void When()
        {
            _result = SystemUnderTest.Get<IDictionary<string, string>>(Key);
        }

        [Then]
        public void ResultIsNull()
        {
            Assert.IsNull(_result);
        }
    }
}
