using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests.FluentNotificationStackTests.AddingMonitoring
{
    public abstract class BaseMonitoringTest : BehaviourTest<FluentNotificationStack>
    {
        protected readonly INotificationStack Stack = Substitute.For<INotificationStack>();

        protected override FluentNotificationStack CreateSystemUnderTest()
        {
            return new FluentNotificationStack(Stack, null);
        }

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }
    }
}