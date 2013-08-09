using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;

namespace Stack.UnitTests.FluentNotificationStackTests.ConfigValidation
{
    public abstract class BaseConfigValidationTest : BehaviourTest<FluentNotificationStack>
    {
        protected override FluentNotificationStack CreateSystemUnderTest()
        {
            return null;
        }

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }
    }
}