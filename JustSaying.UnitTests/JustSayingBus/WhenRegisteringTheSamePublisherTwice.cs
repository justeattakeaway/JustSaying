using JustEat.Testing;
using JustSaying.Models;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringTheSamePublisherTwice : GivenAServiceBus
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<Message>(null);
            SystemUnderTest.AddMessagePublisher<Message>(null);
        }

        [Then]
        public void AnExceptionIsThrown()
        {
            Assert.NotNull(ThrownException);
        }
    }
}