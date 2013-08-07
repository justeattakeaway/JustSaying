using System;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NUnit.Framework;

namespace Stack.UnitTests.FluentNotificationStackTests.Publishing
{
    public class WhenPublishingAndNotInstantiated : BehaviourTest<FluentNotificationStack>
    {
        protected override FluentNotificationStack CreateSystemUnderTest()
        {
            return new FluentNotificationStack(null);
        }

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.Publish(null);
        }

        [Then]
        public void ExceptionIsRaised()
        {
            Assert.IsInstanceOf<InvalidOperationException>(ThrownException);
        }
    }
}
