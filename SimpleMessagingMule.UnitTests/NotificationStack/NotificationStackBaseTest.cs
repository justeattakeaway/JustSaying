using JustEat.Testing;
using NSubstitute;

namespace SimpleMessageMule.UnitTests.NotificationStack
{
    public abstract class NotificationStackBaseTest : BehaviourTest<SimpleMessageMule.NotificationStack>
    {
        protected readonly IMessagingConfig Config = Substitute.For<IMessagingConfig>();

        protected override SimpleMessageMule.NotificationStack CreateSystemUnderTest()
        {
            return new SimpleMessageMule.NotificationStack(Config, null);
        }
    }
}