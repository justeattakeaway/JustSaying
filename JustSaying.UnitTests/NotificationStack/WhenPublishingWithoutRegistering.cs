using System;
using JustSaying.Messaging.Messages;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.NotificationStack
{
    public class WhenPublishingWithoutRegistering : GivenAServiceBus
    {
        protected override void Given()
        {
            base.Given();
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.Publish(Substitute.For<Message>());
        }

        [Then]
        public void InvalidOperationIsThrown()
        {
            Assert.IsInstanceOf<InvalidOperationException>(ThrownException);
        }
    }
}