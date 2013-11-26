namespace Stack.UnitTests.FluentNotificationStackTests.AddingMonitoring
{
    public abstract class BaseMonitoringTest : FluentNotificationStackTestBase
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }
    }
}