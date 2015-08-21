using System;
using JustBehave;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingBus
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
            SystemUnderTest.AddNotificationSubscriber(" ", null);
        }

        [Then]
        public void ArgExceptionThrown()
        {
            Assert.AreEqual(((ArgumentException)ThrownException).ParamName, "region");
        }
    }
}