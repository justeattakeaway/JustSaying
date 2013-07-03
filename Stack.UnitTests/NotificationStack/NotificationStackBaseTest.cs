using JustEat.Testing;
using SimplesNotificationStack.Messaging;

namespace Stack.UnitTests.NotificationStack
{
    public abstract class NotificationStackBaseTest : BehaviourTest<SimplesNotificationStack.Stack.NotificationStack>
    {
        protected override SimplesNotificationStack.Stack.NotificationStack CreateSystemUnderTest()
        {
            return new SimplesNotificationStack.Stack.NotificationStack(Component.BoxHandler);
        }
    }
}