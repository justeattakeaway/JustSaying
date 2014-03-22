using System;
using JustEat.Testing;
using NUnit.Framework;

namespace SimpleMessageMule.UnitTests.NotificationStack
{
    public class WhenSubscribingAndNotPassingATopic : GivenAServiceBus
    {
        protected override void Given()
        {
            base.Given();
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.AddNotificationTopicSubscriber(" ", null);
        }

        [Then]
        public void ArgExceptionThrown()
        {
            Assert.AreEqual(((ArgumentException)ThrownException).ParamName, "topic");
        }
    }
}