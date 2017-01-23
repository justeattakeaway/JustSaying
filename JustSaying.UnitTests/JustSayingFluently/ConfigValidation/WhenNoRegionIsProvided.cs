using System;
using JustBehave;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingFluently.ConfigValidation
{
    public class WhenNoRegionIsProvided : JustSayingFluentlyTestBase
    {
        protected override JustSaying.JustSayingFluently CreateSystemUnderTest()
        {
            return null;
        }

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            CreateMeABus
                .WithNoLogging().InRegion(null)
                .ConfigurePublisherWith(configuration => { });
        }

        [Then]
        public void ConfigItemsAreRequired()
        {
            Assert.IsInstanceOf<ArgumentNullException>(ThrownException);
        }

        [Then]
        public void RegionIsRequested()
        {
            Assert.AreEqual(((ArgumentException)ThrownException).ParamName, "config.Regions");
        }
    }
}