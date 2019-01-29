using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingFluently.ConfigValidation
{
    public class WhenConfigIsValid : JustSayingFluentlyTestBase
    {
        protected override JustSaying.JustSayingFluently CreateSystemUnderTest()
        {
            return null;
        }

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override Task WhenAsync()
        {
            CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion("eu-west-1")
                .ConfigurePublisherWith(configuration => { });

            return Task.CompletedTask;
        }

        [Fact]
        public void NoExceptionIsThrownByConfig()
        {
            ThrownException.ShouldBeNull();
        }
    }
}
