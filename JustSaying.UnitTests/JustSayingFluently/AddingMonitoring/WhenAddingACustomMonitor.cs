using System.Threading.Tasks;
using JustSaying.Messaging.Monitoring;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingFluently.AddingMonitoring
{
    public class WhenAddingACustomMonitor : JustSayingFluentlyTestBase
    {
        readonly IMessageMonitor _monitor = Substitute.For<IMessageMonitor>();
        private object _response;
        protected override Task WhenAction()
        {
            _response = SystemUnderTest.WithMonitoring(_monitor);
            return Task.CompletedTask;
        }

        [Fact]
        public void ThatMonitorIsAddedToTheStack()
        {
            Bus.Received().Monitor = _monitor;
        }

        [Fact]
        public void ICanContinueConfiguringTheBus()
        {
            _response.ShouldBeAssignableTo<IFluentSubscription>();
        }
    }
}
