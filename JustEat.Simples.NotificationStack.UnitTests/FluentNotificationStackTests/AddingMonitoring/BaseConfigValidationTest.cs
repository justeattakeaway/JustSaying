using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests.FluentNotificationStackTests.AddingMonitoring
{
    public abstract class BaseMonitoringTest : BehaviourTest<FluentConfiguration>
    {
        protected readonly INotificationStack Stack = Substitute.For<INotificationStack>();

        protected override FluentConfiguration CreateSystemUnderTest()
        {
            return new FluentConfiguration(Stack);
        }

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }
    }
}