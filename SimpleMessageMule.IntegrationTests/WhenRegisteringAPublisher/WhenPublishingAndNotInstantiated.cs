using System;
using JustEat.Testing;
using NUnit.Framework;
using SimpleMessageMule;
using Tests.MessageStubs;

namespace NotificationStack.IntegrationTests.WhenRegisteringAPublisher
{
    public class WhenRegisteringAPublisherAndNotInstantiated : FluentNotificationStackTestBase
    {
        protected override void Given()
        {
            Configuration = new MessagingConfig
            {
                Region = DefaultRegion.SystemName
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
