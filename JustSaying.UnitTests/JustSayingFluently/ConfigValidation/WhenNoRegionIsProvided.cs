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

        protected override Task WhenAction()
        {
            CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(null)
                .ConfigurePublisherWith(configuration => { });

            return Task.CompletedTask;
        }

        [Fact]
        public void ConfigItemsAreRequired()
        {
            ThrownException.ShouldNotBeNull();
            ThrownException.ShouldBeAssignableTo<InvalidOperationException>();
        }

        [Fact]
        public void RegionIsRequested()
        {
            ThrownException.Message.ShouldBe("Config cannot have a blank entry for the Regions property.");
        }
    }
}
