using System;
using JustEat.Simples.NotificationStack.Messaging.Messages.OrderDispatch;
using JustEat.Testing;
using NUnit.Framework;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenPublishingWithoutRegistering : NotificationStackBaseTest
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.Publish(new OrderAccepted(0, 0, 0));
        }

        [Then]
        public void InvalidOperationIsThrown()
        {
            Assert.IsInstanceOf<InvalidOperationException>(ThrownException);
        }
    }
}