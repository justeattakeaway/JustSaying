using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingFluently.ConfigValidation
{
    public class WhenRegionIsDuplicated : JustSayingFluentlyTestBase
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
                .WithLogging(new LoggerFactory())
                .InRegions("dup1", "andalso2", "uniq", "andalso2", "dup1")
                .ConfigurePublisherWith(configuration => { });

            return Task.CompletedTask;
        }

        [Fact]
        public void DuplicateRegionsAreRejectedWithException()
        {
            ThrownException.ShouldNotBeNull();
            ThrownException.ShouldBeAssignableTo<InvalidOperationException>();
        }

        [Fact]
        public void ExceptionMessageListsDuplicateRegions()
        {
            ThrownException.Message.ShouldBe("Config has duplicates in Regions for 'dup1,andalso2'.");
        }
    }
}
