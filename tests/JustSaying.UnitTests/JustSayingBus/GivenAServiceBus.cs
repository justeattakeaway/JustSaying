using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
{
    public abstract class GivenAServiceBus : IAsyncLifetime
    {
        protected IMessagingConfig Config;
        protected IMessageMonitor Monitor;
        protected ILoggerFactory LoggerFactory;
        private bool _recordThrownExceptions;

        protected Exception ThrownException { get; private set; }

        protected JustSaying.JustSayingBus SystemUnderTest { get; private set; }

        protected static readonly TimeSpan TimeoutPeriod = TimeSpan.FromSeconds(1);

        public virtual async Task InitializeAsync()
        {
            Given();

            try
            {
                SystemUnderTest = CreateSystemUnderTest();
                await WhenAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (_recordThrownExceptions)
            {
                ThrownException = ex;
            }
        }

        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void Given()
        {
            Config = Substitute.For<IMessagingConfig>();
            Monitor = Substitute.For<IMessageMonitor>();
            LoggerFactory = Substitute.For<ILoggerFactory>();
        }

        protected abstract Task WhenAsync();

        private JustSaying.JustSayingBus CreateSystemUnderTest()
        {
            var subjectProvider = new GenericMessageSubjectProvider();
            var serializerFactory = new NewtonsoftSerializationFactory();
            var bus = new JustSaying.JustSayingBus(Config,
                new MessageSerializationRegister(subjectProvider, serializerFactory),
                LoggerFactory)
            {
                Monitor = Monitor
            };

            bus.SetGroupSettings(new SubscriptionGroupSettingsBuilder()
                    .WithDefaultConcurrencyLimit(8),
                new Dictionary<string, SubscriptionGroupConfigBuilder>());

            return bus;
        }

        public void RecordAnyExceptionsThrown()
        {
            _recordThrownExceptions = true;
        }
    }
}
