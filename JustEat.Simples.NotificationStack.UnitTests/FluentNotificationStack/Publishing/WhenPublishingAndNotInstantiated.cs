using System;
using JustEat.Testing;
using NUnit.Framework;

namespace Stack.UnitTests.FluentNotificationStack.Publishing
{
    public class WhenPublishingAndNotInstantiated : BehaviourTest<JustEat.Simples.NotificationStack.Stack.FluentNotificationStack>
    {
        protected override JustEat.Simples.NotificationStack.Stack.FluentNotificationStack CreateSystemUnderTest()
        {
            return new JustEat.Simples.NotificationStack.Stack.FluentNotificationStack(null, null);
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
