using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests.FluentNotificationStackTests.AddingMonitoring
{
    public class WhenAddingACustomMonitor : BaseMonitoringTest
    {
        readonly IMessageMonitor _monitor = Substitute.For<IMessageMonitor>();

        protected override void When()
        {
            SystemUnderTest.WithMonitoring(_monitor);
        }

        [Then]
        public void ThatMonitorIsAddedToTheStack()
        {
            Stack.Received().Monitor = _monitor;
        }
    }
}