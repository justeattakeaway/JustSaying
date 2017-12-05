using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

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

        protected override Task When()
        {
            CreateMeABus
                .WithLogging(new LoggerFactory()).InRegion(null)
                .ConfigurePublisherWith(configuration => { });
            return Task.CompletedTask;
        }

        [Fact]
        public void ConfigItemsAreRequired()
        {
            ThrownException.ShouldBeAssignableTo<ArgumentNullException>();
        }

        [Fact]
        public void RegionIsRequested()
        {
            ((ArgumentException)ThrownException).ParamName.ShouldBe("config.Regions");
        }
    }
}
