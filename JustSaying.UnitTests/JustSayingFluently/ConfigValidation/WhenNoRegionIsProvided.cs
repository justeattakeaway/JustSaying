using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingFluently.ConfigValidation
{
    public class WhenNoRegionIsProvided : JustSayingFluentlyTestBase
    {
        protected override Task<JustSaying.JustSayingFluently> CreateSystemUnderTestAsync()
        {
            return Task.FromResult<JustSaying.JustSayingFluently>(null);
        }

        protected override Task Given()
        {
            RecordAnyExceptionsThrown();
            return Task.CompletedTask;
        }

        protected override Task When()
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
