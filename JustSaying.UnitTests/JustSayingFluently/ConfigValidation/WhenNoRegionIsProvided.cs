using System;
using System.Threading.Tasks;
using JustBehave;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingFluently.ConfigValidation
{
    public class WhenNoRegionIsProvided : JustSayingFluentlyTestBase
    {
        protected override JustSaying.JustSayingFluently CreateSystemUnderTest() => null;

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override async Task When()
        {
            await CreateMeABus
                .WithLogging(new LoggerFactory()).InRegion(null)
                .ConfigurePublisherWith(configuration => { })
                .BuildPublisherAsync();
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