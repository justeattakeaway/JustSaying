using System;
using JustBehave;
using JustSaying.TestingFramework;
using NUnit.Framework;

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

        protected override void When()
        {
            SystemUnderTest.Publish(new GenericMessage());
        }

        [Then]
        public void ExceptionIsRaised()
        {
            Assert.IsInstanceOf<InvalidOperationException>(ThrownException);
        }
    }
}
