using JustBehave;
using JustSaying.Messaging.Monitoring;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
{
    public abstract class GivenAServiceBus : BehaviourTest<JustSaying.JustSayingBus>
    {
        protected IMessagingConfig Config;
        protected IPublishConfiguration PublishConfig;
        protected IMessageMonitor Monitor;

        protected override void Given()
        {
            Config = Substitute.For<IMessagingConfig>();
            PublishConfig = Substitute.For<IPublishConfiguration>();
            Monitor = Substitute.For<IMessageMonitor>();
        }

        protected override JustSaying.JustSayingBus CreateSystemUnderTest()
        {
            return new JustSaying.JustSayingBus(Config, PublishConfig,  null) {Monitor = Monitor};
        }
    }
}