using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.MessageReceiveBufferTests
{
    public abstract class BaseMessageReceiveBufferTests : IAsyncLifetime
    {
        protected ISqsQueue Queue;
        protected IMessageMonitor Monitor;
        protected readonly ILoggerFactory LoggerFactory;

        protected IMessageReceiveBuffer SystemUnderTest { get; private set; }

        public BaseMessageReceiveBufferTests(ITestOutputHelper testOutputHelper)
        {
            LoggerFactory = testOutputHelper.ToLoggerFactory();
        }

        public async Task InitializeAsync()
        {
            GivenInternal();

            SystemUnderTest = CreateSystemUnderTest();

            await WhenInternalAsync().ConfigureAwait(false);
        }

        private void GivenInternal()
        {
            Queue = Substitute.For<ISqsQueue>();
            Monitor = Substitute.For<IMessageMonitor>();

            Given();
        }

        protected abstract void Given();
        protected abstract Task WhenAsync();

        // Default implementation
        private async Task WhenInternalAsync()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(1));

            await WhenAsync();

            // wait until it's done
            var doneOk = await TaskHelpers.WaitWithTimeoutAsync(
                StartAndCatch(cts.Token));

            doneOk.ShouldBeTrue("Timeout occured before done signal");
        }

        private async Task StartAndCatch(CancellationToken cancellationToken)
        {
            try
            {
                await SystemUnderTest.Start(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            { }
        }

        protected IMessageReceiveBuffer CreateSystemUnderTest()
        {
            return new MessageReceiveBuffer(
                10,
                Queue,
                Monitor,
                LoggerFactory);
        }

        public Task DisposeAsync()
        {
            LoggerFactory?.Dispose();

            return Task.CompletedTask;
        }

        protected class TestMessage : Amazon.SQS.Model.Message { }
    }
}
