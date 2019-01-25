using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenSubscribingAndNotPassingATopic : GivenAServiceBus
    {
        protected override void Given()
        {
            base.Given();
            RecordAnyExceptionsThrown();
        }

        protected override Task WhenAction()
        {
            SystemUnderTest.AddNotificationSubscriber(" ", null);
            return Task.CompletedTask;
        }

        [Fact]
        public void ArgExceptionThrown()
        {
            ((ArgumentException) ThrownException).ParamName.ShouldBe("region");
        }
    }
}
