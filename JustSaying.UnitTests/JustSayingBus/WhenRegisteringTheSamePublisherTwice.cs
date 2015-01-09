using JustBehave;
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
            SystemUnderTest.AddMessagePublisher<Message>(null, string.Empty);
            SystemUnderTest.AddMessagePublisher<Message>(null, string.Empty);
        }

        [Then]
        public void AnExceptionIsThrown()
        {
            Assert.NotNull(ThrownException);
        }
    }
}