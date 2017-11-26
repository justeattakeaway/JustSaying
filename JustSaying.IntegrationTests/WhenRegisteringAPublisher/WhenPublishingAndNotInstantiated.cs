using System;
using System.Threading.Tasks;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringAPublisher
{
    public class WhenRegisteringAPublisherAndNotInstantiated : FluentNotificationStackTestBase
    {
        protected override void Given()
        {
            base.Given();

            Configuration = new MessagingConfig();

            RecordAnyExceptionsThrown();
        }

        protected override async Task When()
        {
            await SystemUnderTest.PublishAsync(new GenericMessage());
        }

        [Fact]
        public void ExceptionIsRaised()
        {
            ThrownException.ShouldBeAssignableTo<InvalidOperationException>();
        }
    }
}
