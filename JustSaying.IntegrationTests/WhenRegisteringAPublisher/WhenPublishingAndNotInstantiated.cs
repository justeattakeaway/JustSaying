using System;
using System.Threading.Tasks;
using JustBehave;
using JustSaying.TestingFramework;
using Xunit;
using Assert = NUnit.Framework.Assert;

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
            Assert.IsInstanceOf<InvalidOperationException>(ThrownException);
        }
    }
}
