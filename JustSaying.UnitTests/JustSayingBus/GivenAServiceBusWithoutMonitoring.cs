using JustBehave;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
{
    public abstract class GivenAServiceBusWithoutMonitoring : BehaviourTest<JustSaying.JustSayingBus>
    {
        protected IMessagingConfig Config;
        protected IMessageMonitor Monitor;
        protected ILoggerFactory LoggerFactory;

        protected override void Given()
        {
            Config = Substitute.For<IMessagingConfig>();
            LoggerFactory = Substitute.For<ILoggerFactory>();
        }

        protected override JustSaying.JustSayingBus CreateSystemUnderTest()
            => new JustSaying.JustSayingBus(Config, null, LoggerFactory);
    }
}