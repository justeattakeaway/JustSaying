using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingFluently.ConfigValidation
{
    public class WhenConfigIsValid : JustSayingFluentlyTestBase
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
