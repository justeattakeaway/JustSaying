using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public abstract class GivenAServiceBusWithoutMonitoring : IAsyncLifetime
    {
        protected IMessagingConfig Config;
        protected ILoggerFactory LoggerFactory;

        protected JustSaying.JustSayingBus SystemUnderTest { get; private set; }

        public virtual async Task InitializeAsync()
        {
            Given();

            SystemUnderTest = CreateSystemUnderTest();
            await WhenAsync().ConfigureAwait(false);
        }


        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void Given()
        {
            Config = Substitute.For<IMessagingConfig>();
            LoggerFactory = Substitute.For<ILoggerFactory>();
        }

        protected abstract Task WhenAsync();

        private JustSaying.JustSayingBus CreateSystemUnderTest()
        {
            return new JustSaying.JustSayingBus(Config, null, LoggerFactory, null);
        }
    }
}
