using System;
using System.Reflection;
using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests
{
    public abstract class JustSayingFluentlyTestBase : IAsyncLifetime
    {
        protected IPublishConfiguration Configuration;
        protected IAmJustSaying Bus;
        protected readonly IVerifyAmazonQueues QueueVerifier = Substitute.For<IVerifyAmazonQueues>();
        private bool _recordThrownExceptions;
        protected Exception ThrownException { get; private set; }

        public JustSaying.JustSayingFluently SystemUnderTest { get; private set; }

        protected virtual JustSaying.JustSayingFluently CreateSystemUnderTest()
        {
            if (Configuration == null)
            {
                Configuration = new MessagingConfig();
            }

            var fns = CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion("defaultRegion")
                .WithFailoverRegion("failoverRegion")
                .WithActiveRegion(() => "defaultRegion")
                .ConfigurePublisherWith(x =>
                {
                    x.PublishFailureBackoff = Configuration.PublishFailureBackoff;
                    x.PublishFailureReAttempts = Configuration.PublishFailureReAttempts;

                }) as JustSaying.JustSayingFluently;

            ConfigureNotificationStackMock(fns);
            ConfigureAmazonQueueCreator(fns);

            return fns;
        }

        // ToDo: Must do better!!
        private void ConfigureNotificationStackMock(JustSaying.JustSayingFluently fns)
        {
            var constructedStack = (JustSaying.JustSayingBus)fns.Bus;

            Bus = Substitute.For<IAmJustSaying>();
            Bus.Config.Returns(constructedStack.Config);

            fns.Bus = Bus;
        }

        private void ConfigureAmazonQueueCreator(JustSaying.JustSayingFluently fns)
        {
            fns.GetType()
                .GetField("_amazonQueueCreator", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(fns, QueueVerifier);
        }

        public async Task InitializeAsync()
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

        protected virtual void Given()
        {

        }

        protected abstract Task WhenAsync();

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public void RecordAnyExceptionsThrown()
        {
            _recordThrownExceptions = true;
        }
    }
}
