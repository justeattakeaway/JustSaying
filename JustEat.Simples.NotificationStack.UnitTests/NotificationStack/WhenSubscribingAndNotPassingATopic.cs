using System;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Testing;
using NUnit.Framework;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenSubscribingAndNotPassingATopic : NotificationStackBaseTest
    {
        private INotificationSubscriber _subscriber1;

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber(" ", _subscriber1);
        }

        [Then]
        public void ArgExceptionThrown()
        {
            Assert.AreEqual(((ArgumentException)ThrownException).ParamName, "topic");
        }
    }
}