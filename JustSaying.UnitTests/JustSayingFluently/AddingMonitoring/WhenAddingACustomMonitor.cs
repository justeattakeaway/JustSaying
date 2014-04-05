using JustEat.Testing;
using JustSaying.Messaging.Monitoring;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingFluently.AddingMonitoring
{
    public class WhenAddingACustomMonitor : FluentMessageMuleTestBase
    {
        readonly IMessageMonitor _monitor = Substitute.For<IMessageMonitor>();
        private object _response;

        protected override void Given() { }

        protected override void When()
        {
            _response = SystemUnderTest.WithMonitoring(_monitor);
        }

        [Then]
        public void ThatMonitorIsAddedToTheStack()
        {
            NotificationStack.Received().Monitor = _monitor;
        }

        [Then]
        public void ICanContinueConfiguringTheBus()
        {
            Assert.IsInstanceOf<IFluentSubscription>(_response);
        }
    }
}