using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
{
    public class WhenThereAreNoMessagesToProcess : IAsyncLifetime
    {
        private IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private int _callCount;
        protected JustSaying.AwsTools.MessageHandling.SqsNotificationListener SystemUnderTest { get; private set; }

        private JustSaying.AwsTools.MessageHandling.SqsNotificationListener CreateSystemUnderTest()
        {
            var listener = new JustSaying.AwsTools.MessageHandling.SqsNotificationListener(
                new SqsQueueByUrl(RegionEndpoint.EUWest1, new Uri("http://foo.com"), _sqs, NullLoggerFactory.Instance),
                null,
                Substitute.For<IMessageMonitor>(),
                Substitute.For<ILoggerFactory>(),
                Substitute.For<IMessageContextAccessor>());
            return listener;
        }

        public virtual async Task InitializeAsync()
        {
            Given();

            SystemUnderTest = CreateSystemUnderTest();

            await When().ConfigureAwait(false);
        }

        public virtual Task DisposeAsync()
        {
            if (_sqs != null)
            {
                _sqs.Dispose();
                _sqs = null;
            }

            return Task.CompletedTask;
        }

        private void Given()
        {
            _sqs.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult(GenerateEmptyMessage()));

            _sqs.When(x => x.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>()))
                .Do(x => _callCount++);
        }

        private async Task When()
        {
            var cts = new CancellationTokenSource();
            SystemUnderTest.Listen(cts.Token);
            await Task.Delay(100);
            cts.Cancel();
        }

        [Fact]
        public void ListenLoopDoesNotDie()
        {
            _callCount.ShouldBeGreaterThan(3);
        }

        private static ReceiveMessageResponse GenerateEmptyMessage()
        {
            return new ReceiveMessageResponse { Messages = new List<Message>() };
        }
    }
}
