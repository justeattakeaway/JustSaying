using System.Threading.Tasks;
using JustBehave;
using JustSaying.Messaging;
using JustSaying.Messaging.Monitoring;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingFluently.AddingMonitoring
{
    public class WhenAddingACustomMonitor : JustSayingFluentlyTestBase
    {
        readonly IMessageMonitor _monitor = Substitute.For<IMessageMonitor>();
        private IMessagePublisher _response;

        protected override void Given() { }

        protected override async Task When()
        {
            _response = await SystemUnderTest.WithMonitoring(_monitor).ConfigurePublisherWith(x => {}).BuildPublisherAsync();
        }

        [Then]
        public void ThatMonitorIsAddedToTheStack()
        {
            Bus.Received().Monitor = _monitor;
        }

        [Then]
        public void ICanContinueConfiguringTheBus()
        {
            Assert.IsInstanceOf<IFluentSubscription>(_response);
        }
    }
}