using System;
using JustSaying.Stack;
using JustEat.Testing;
using JustSaying.Tests.MessageStubs;
using NUnit.Framework;

namespace NotificationStack.IntegrationTests.WhenRegisteringAPublisher
{
    public class WhenRegisteringAPublisherAndNotInstantiated : FluentNotificationStackTestBase
    {
        protected override void Given()
        {
            Configuration = new MessagingConfig
            {
                Component = "intergrationtestcomponent",
                Environment = "integrationtest",
                Tenant = "all"
            };

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
