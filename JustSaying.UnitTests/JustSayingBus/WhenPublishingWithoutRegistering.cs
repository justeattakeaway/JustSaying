using System;
using JustEat.Testing;
using JustSaying.Messaging.Messages;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingBus
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