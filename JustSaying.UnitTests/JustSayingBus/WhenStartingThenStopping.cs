using System.Threading.Tasks;
using JustBehave;
using JustSaying.Messaging;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenStartingThenStopping : GivenAServiceBus
    {
        private INotificationSubscriber _subscriber1;

        protected override void Given()
        {
            base.Given();
            _subscriber1 = Substitute.For<INotificationSubscriber>();
        }

        protected override Task When()
        {
            SystemUnderTest.AddNotificationSubscriber("region1", _subscriber1);
            SystemUnderTest.Start();
            SystemUnderTest.Stop();
            return Task.FromResult(true);
        }

        [Then]
        public void StateIsNotListening()
        {
            Assert.False(SystemUnderTest.Listening);
        }

        [Then]
        public void CallingStopTwiceDoesNotStopListeningTwice()
        {
            SystemUnderTest.Stop();
            _subscriber1.Received(1).StopListening();
        }
    }
}