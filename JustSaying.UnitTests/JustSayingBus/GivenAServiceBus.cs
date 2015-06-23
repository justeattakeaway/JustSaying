using JustBehave;
using JustSaying.Extensions;
using JustSaying.Messaging.Monitoring;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
{
    public abstract class GivenAServiceBus : BehaviourTest<JustSaying.JustSayingBus>
    {
        protected IMessagingConfig Config;
        protected IMessageMonitor Monitor;

        protected override void Given()
        {
            Config = Substitute.For<IMessagingConfig>();
            Config.TopicNameProvider = t => t.ToTopicName();
            Monitor = Substitute.For<IMessageMonitor>();
        }

        protected override JustSaying.JustSayingBus CreateSystemUnderTest()
        {
            return new JustSaying.JustSayingBus(Config, null) {Monitor = Monitor};
        }
    }
}