using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using JustEat.Testing;
using NSubstitute;

namespace SimpleMessageMule.UnitTests.SimpleMessageMuleTests.AddingMonitoring
{
    public class WhenAddingACustomMonitor : FluentMessageMuleTestBase
    {
        readonly IMessageMonitor _monitor = Substitute.For<IMessageMonitor>();

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.WithMonitoring(_monitor);
        }

        [Then]
        public void ThatMonitorIsAddedToTheStack()
        {
            NotificationStack.Received().Monitor = _monitor;
        }
    }
}