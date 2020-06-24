using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Middleware;
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
        protected MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>> SqsMiddleware;

        internal IMessageReceiveBuffer SystemUnderTest { get; private set; }

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
            SqsMiddleware = new DelegateMiddleware<GetMessagesContext, IList<Amazon.SQS.Model.Message>>();

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
                await SystemUnderTest.RunAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            { }
        }

        internal IMessageReceiveBuffer CreateSystemUnderTest()
        {
            return new MessageReceiveBuffer(
                10,
                10,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                Queue,
                SqsMiddleware,
                Monitor,
                LoggerFactory.CreateLogger<IMessageReceiveBuffer>());
        }

        public Task DisposeAsync()
        {
            LoggerFactory?.Dispose();

            return Task.CompletedTask;
        }

        protected class TestMessage : Amazon.SQS.Model.Message { }
    }
}
