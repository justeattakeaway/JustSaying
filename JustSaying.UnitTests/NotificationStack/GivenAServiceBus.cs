using JustSaying.Messaging.Monitoring;
using JustEat.Testing;
using NSubstitute;

namespace JustSaying.UnitTests.NotificationStack
{
    public abstract class GivenAServiceBus : BehaviourTest<JustSaying.NotificationStack>
    {
        protected IMessagingConfig Config;
        protected IMessageMonitor Monitor;

        protected override void Given()
        {
            Config = Substitute.For<IMessagingConfig>();
            Monitor = Substitute.For<IMessageMonitor>();
        }

        protected override JustSaying.NotificationStack CreateSystemUnderTest()
        {
            return new JustSaying.NotificationStack(Config, null) {Monitor = Monitor};
        }
    }
}