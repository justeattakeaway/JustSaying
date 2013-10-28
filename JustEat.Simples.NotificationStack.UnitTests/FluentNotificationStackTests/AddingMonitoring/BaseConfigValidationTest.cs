using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests.FluentNotificationStackTests.AddingMonitoring
{
    public abstract class BaseMonitoringTest : BehaviourTest<FluentMonitoring>
    {
        protected readonly INotificationStack Stack = Substitute.For<INotificationStack>();

        protected override FluentMonitoring CreateSystemUnderTest()
        {
            return new FluentMonitoring(Stack);
        }

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }
    }
}