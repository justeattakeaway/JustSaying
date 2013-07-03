using JustEat.Testing;
using JustEat.Simples.NotificationStack.Messaging;

namespace Stack.UnitTests.NotificationStack
{
    public abstract class NotificationStackBaseTest : BehaviourTest<JustEat.Simples.NotificationStack.Stack.NotificationStack>
    {
        protected override JustEat.Simples.NotificationStack.Stack.NotificationStack CreateSystemUnderTest()
        {
            return new JustEat.Simples.NotificationStack.Stack.NotificationStack(Component.BoxHandler);
        }
    }
}