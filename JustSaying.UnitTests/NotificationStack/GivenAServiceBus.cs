using JustSaying.Messaging.Monitoring;
using JustEat.Testing;
using NSubstitute;

namespace JustSaying.UnitTests.NotificationStack
{
    public abstract class GivenAServiceBus : BehaviourTest<JustSaying.JustSayingBus>
    {
        protected IMessagingConfig Config;
        protected IMessageMonitor Monitor;

        protected override void Given()
        {
            Config = Substitute.For<IMessagingConfig>();
            Monitor = Substitute.For<IMessageMonitor>();
        }

        protected override JustSaying.JustSayingBus CreateSystemUnderTest()
        {
            return new JustSaying.JustSayingBus(Config, null) {Monitor = Monitor};
        }
    }
}