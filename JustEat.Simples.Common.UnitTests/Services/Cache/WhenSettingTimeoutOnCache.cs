using System;
using System.Threading;
using JustEat.Testing;
using NUnit.Framework;

namespace JustEat.Simples.Common.UnitTests.Services.Cache
{
    public class WhenSettingTimeoutOnInProcCache : WhenSettingTimeoutOnInProcCacheBase
    {
        protected override void When()
        {
            Thread.Sleep(new TimeSpan(0, 0, 0, Timeout + 1));        
            Result = SystemUnderTest.Get<CacheableClass>(Key);
        }

        [Then]
        public void ResultIsNull()
        {
            Assert.IsNull(Result);
        }
    }
}