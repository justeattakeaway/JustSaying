using JustEat.Simples.NotificationStack.Stack.Monitoring;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests.FluentNotificationStackTests.AddingMonitoring
{
    public class WhenAddingAStatsDMonitor : BaseMonitoringTest
    {
        protected override void When()
        {
            SystemUnderTest.WithStatsDMonitoring(null);
        }

        [Then]
        public void ThenAStatsDMonitorIsProvided()
        {
            Stack.Received().Monitor = Arg.Any<StatsDMessageMonitor>();
        }
    }
}