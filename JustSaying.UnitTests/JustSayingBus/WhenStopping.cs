using System.Threading.Tasks;
using JustBehave;
using JustSaying.Messaging;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenStopping : GivenAServiceBus
    {
        private INotificationSubscriber _subscriber1;
        private INotificationSubscriber _subscriber2;

        protected override void Given()
        {
            base.Given();
            _subscriber1 = Substitute.For<INotificationSubscriber>();
            _subscriber1.Queue.Returns("queue1");
            _subscriber2 = Substitute.For<INotificationSubscriber>();
            _subscriber2.Queue.Returns("queue2");
        }

        protected override Task When()
        {
            SystemUnderTest.AddNotificationSubscriber("region1", _subscriber1);
            SystemUnderTest.AddNotificationSubscriber("region1", _subscriber2);
            SystemUnderTest.Start();
            SystemUnderTest.Stop();

            return Task.CompletedTask;
        }

        [Then]
        public void SubscribersAreToldToStopListening()
        {
            _subscriber1.Received().StopListening();
            _subscriber2.Received().StopListening();
        }
    }
}
