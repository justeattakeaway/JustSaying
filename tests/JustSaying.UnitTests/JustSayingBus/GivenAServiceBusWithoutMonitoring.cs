using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public abstract class GivenAServiceBusWithoutMonitoring
{
    protected IMessagingConfig Config;
    protected ILoggerFactory LoggerFactory;

    protected JustSaying.JustSayingBus SystemUnderTest { get; private set; }

    [Before(Test)]
    public virtual async Task SetUp()
    {
        Given();

        SystemUnderTest = CreateSystemUnderTest();
        await WhenAsync().ConfigureAwait(false);
    }

    protected virtual void Given()
    {
        Config = Substitute.For<IMessagingConfig>();
        LoggerFactory = Substitute.For<ILoggerFactory>();
    }

    protected abstract Task WhenAsync();

    private JustSaying.JustSayingBus CreateSystemUnderTest()
    {
        return new JustSaying.JustSayingBus(Config, new NewtonsoftSerializationFactory(), new MessageReceivePauseSignal(), LoggerFactory, null);
    }
}
