using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using JustEat.Testing;
using NSubstitute;

namespace SimpleMessageMule.UnitTests.NotificationStack
{
    public abstract class NotificationStackBaseTest : BehaviourTest<SimpleMessageMule.NotificationStack>
    {
        protected IMessagingConfig Config;
        protected IMessageMonitor Monitor;

        protected override void Given()
        {
            
            Config = Substitute.For<IMessagingConfig>();
            Monitor = Substitute.For<IMessageMonitor>();
        }

        protected override SimpleMessageMule.NotificationStack CreateSystemUnderTest()
        {
            return new SimpleMessageMule.NotificationStack(Config, null) {Monitor = Monitor};
        }
    }
}