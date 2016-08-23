using JustBehave;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
{
    public abstract class GivenAServiceBus : AsyncBehaviourTest<JustSaying.JustSayingBus>
    {
        protected IMessagingConfig Config;
        protected IMessageMonitor Monitor;

        protected override void Given()
        {
            Config = Substitute.For<IMessagingConfig>();
            Monitor = Substitute.For<IMessageMonitor>();

            Logging.ToConsole();
        }

        protected override JustSaying.JustSayingBus CreateSystemUnderTest()
            => new JustSaying.JustSayingBus(Config, null) {Monitor = Monitor};
    }
}