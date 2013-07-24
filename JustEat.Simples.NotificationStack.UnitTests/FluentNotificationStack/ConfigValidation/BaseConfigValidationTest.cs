using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests.FluentNotificationStack.ConfigValidation
{
    public abstract class BaseConfigValidationTest : BehaviourTest<JustEat.Simples.NotificationStack.Stack.FluentNotificationStack>
    {
        protected readonly IMessagingConfig Config = Substitute.For<IMessagingConfig>();

        protected override JustEat.Simples.NotificationStack.Stack.FluentNotificationStack CreateSystemUnderTest()
        {
            return null;
        }

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            JustEat.Simples.NotificationStack.Stack.FluentNotificationStack.Register(Component.OrderEngine, Config);
        }
    }
}