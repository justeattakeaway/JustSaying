using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using JustSaying.UnitTests.Messaging.Policies.ExamplePolicies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Policies
{
    public class ChannelPolicyTests
    {
        private ILoggerFactory LoggerFactory { get; }
        private IMessageMonitor MessageMonitor { get; }


        public ChannelPolicyTests(ITestOutputHelper testOutputHelper)
        {
            LoggerFactory = testOutputHelper.ToLoggerFactory();
            MessageMonitor = new TrackingLoggingMonitor(LoggerFactory.CreateLogger<IMessageMonitor>());
        }

        private static readonly TimeSpan TimeoutPeriod = TimeSpan.FromSeconds(1);

        [Fact]
        public async Task ErrorHandlingAroundSqs()
        {
            // Arrange
            int queueCalledCount = 0;
            int dispatchedMessageCount = 0;
            var sqsQueue = TestQueue(() => Interlocked.Increment(ref queueCalledCount));

            var queues = new List<ISqsQueue> { sqsQueue };

            var config = new SubscriptionGroupSettingsBuilder()
                .WithDefaultConcurrencyLimit(8);
            config.WithCustomMiddleware(
                new ErrorHandlingMiddleware<GetMessagesContext, IList<Message>, InvalidOperationException>());

            var settings = new Dictionary<string, SubscriptionGroupConfigBuilder>
            {
                { "test", new SubscriptionGroupConfigBuilder("test").AddQueues(queues) },
            };

            IMessageDispatcher dispatcher = new FakeDispatcher(() => Interlocked.Increment(ref dispatchedMessageCount));

            var groupFactory = new SubscriptionGroupFactory(
                dispatcher,
                MessageMonitor,
                LoggerFactory);

            ISubscriptionGroup collection = groupFactory.Create(config, settings);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            // Act and Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => collection.RunAsync(cts.Token));

            queueCalledCount.ShouldBeGreaterThan(1);
            dispatchedMessageCount.ShouldBe(0);
        }

        private static ISqsQueue TestQueue(Action spy = null)
        {
            async Task<ReceiveMessageResponse> GetMessages()
            {
                spy?.Invoke();
                await Task.Delay(TimeSpan.FromMilliseconds(5)).ConfigureAwait(false);
                throw new InvalidOperationException();
            }

            ISqsQueue sqsQueueMock = Substitute.For<ISqsQueue>();
            sqsQueueMock.Uri.Returns(new Uri("http://test.com"));
            sqsQueueMock
                .Client
                .ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(async _ => await GetMessages());

            return sqsQueueMock;
        }
    }
}
