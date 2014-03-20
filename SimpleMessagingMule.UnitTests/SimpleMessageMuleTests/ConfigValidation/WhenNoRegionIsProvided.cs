using System;
using JustEat.Testing;
using NUnit.Framework;

namespace SimpleMessageMule.UnitTests.SimpleMessageMuleTests.ConfigValidation
{
    public class WhenNoRegionIsProvided : FluentMessageMuleTestBase
    {
        protected override FluentMessagingMule CreateSystemUnderTest()
        {
            return null;
        }

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            FluentMessagingMule.Register(configuration => { });
        }

        [Then]
        public void ConfigItemsAreRequired()
        {
            Assert.IsInstanceOf<ArgumentNullException>(ThrownException);
        }

        [Then]
        public void RegionIsRequested()
        {
            Assert.AreEqual(((ArgumentException)ThrownException).ParamName, "config.Region");
        }
    }
}