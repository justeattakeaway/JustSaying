using System;
using System.Threading.Tasks;
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

        protected override Task When()
        {
            SystemUnderTest.AddNotificationSubscriber(" ", null);
            return Task.CompletedTask;
        }

        [Then]
        public void ArgExceptionThrown()
        {
            Assert.AreEqual(((ArgumentException)ThrownException).ParamName, "region");
        }
    }
}
