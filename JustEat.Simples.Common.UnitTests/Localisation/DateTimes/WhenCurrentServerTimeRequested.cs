using System;
using JustEat.Simples.Common.Localisation.DateTimeProviders;
using JustEat.Testing;
using NUnit.Framework;

namespace JustEat.Simples.Common.UnitTests.Localisation.DateTimes
{
    public class WhenCurrentServerTimeRequested : BehaviourTest<UtcTime>
    {
        private DateTime _result;

        protected override void Given() { }

        protected override void When()
        {
            _result = SystemUnderTest.Now;
        }

        [Then]
        public void TimeIsReturned()
        {
            Assert.NotNull(_result);
        }

        [Then]
        public void TimeIsUtc()
        {
            Assert.That(_result.Kind, Is.EqualTo(DateTimeKind.Utc));
        }
    }
}