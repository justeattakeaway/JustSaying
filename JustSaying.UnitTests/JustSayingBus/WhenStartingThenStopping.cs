using System.Threading.Tasks;
using JustSaying.Messaging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenStartingThenStopping : GivenAServiceBus
    {
        private INotificationSubscriber _subscriber1;

        protected override async Task Given()
        {
            await base.Given();
            _subscriber1 = Substitute.For<INotificationSubscriber>();
        }

        protected override Task When()
        {
            SystemUnderTest.AddNotificationSubscriber("region1", _subscriber1);
            SystemUnderTest.Start();
            SystemUnderTest.Stop();

            return Task.CompletedTask;
        }

        [Fact]
        public void StateIsNotListening()
        {
            SystemUnderTest.Listening.ShouldBeFalse();
        }

        [Fact]
        public void CallingStopTwiceDoesNotStopListeningTwice()
        {
            SystemUnderTest.Stop();
            _subscriber1.Received(1).StopListening();
        }
    }
}
