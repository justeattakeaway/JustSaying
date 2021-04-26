using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Middleware.Receive;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using JustSaying.UnitTests.Messaging.Policies.ExamplePolicies;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Policies
{
    public class ChannelPolicyTests
    {
        private ILoggerFactory LoggerFactory { get; }
        private IMessageMonitor MessageMonitor { get; }
        private readonly ITestOutputHelper _outputHelper;

        public ChannelPolicyTests(ITestOutputHelper testOutputHelper)
        {
            _outputHelper = testOutputHelper;
            LoggerFactory = testOutputHelper.ToLoggerFactory();
            MessageMonitor = new TrackingLoggingMonitor(LoggerFactory.CreateLogger<TrackingLoggingMonitor>());
        }

        [Fact]
        public async Task ErrorHandlingAroundSqs_WithCustomPolicy_CanSwallowExceptions()
        {
            // Arrange
            int queueCalledCount = 0;
            int dispatchedMessageCount = 0;
            var sqsQueue = TestQueue(() => Interlocked.Increment(ref queueCalledCount));

            var queues = new List<ISqsQueue> { sqsQueue };

            var config = new SubscriptionGroupSettingsBuilder()
                .WithDefaultConcurrencyLimit(8);
            config.WithCustomMiddleware(
                new ErrorHandlingMiddleware<ReceiveMessagesContext, IList<Message>, InvalidOperationException>());

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
            var completion = collection.RunAsync(cts.Token);

            await Patiently.AssertThatAsync(_outputHelper,
                () =>
                {
                    queueCalledCount.ShouldBeGreaterThan(1);
                    dispatchedMessageCount.ShouldBe(0);
                });

            cts.Cancel();
            // Act and Assert

            await completion.HandleCancellation();
        }

        private static ISqsQueue TestQueue(Action spy = null)
        {
            ReceiveMessageResponse GetMessages()
            {
                spy?.Invoke();
                throw new InvalidOperationException();
            }

            var sqs = new FakeAmazonSqs(() => GetMessages().Infinite());
            var queue = new FakeSqsQueue("test-queue", sqs);

            return queue;
        }
    }
}
