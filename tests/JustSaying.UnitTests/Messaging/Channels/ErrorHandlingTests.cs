using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels
{
    public class ErrorHandlingTests
    {
        public ILoggerFactory LoggerFactory { get; }
        private IMessageMonitor MessageMonitor { get; }

        private readonly ITestOutputHelper _outputHelper;

        public ErrorHandlingTests(ITestOutputHelper testOutputHelper)
        {
            _outputHelper = testOutputHelper;
            LoggerFactory = testOutputHelper.ToLoggerFactory();
            MessageMonitor = new TrackingLoggingMonitor(LoggerFactory.CreateLogger<IMessageMonitor>());
        }

        [Fact]
        public async Task Sqs_Client_Throwing_Exceptions_Continues_To_Request_Messages()
        {
            // Arrange
            int messagesRequested = 0;
            int messagesDispatched = 0;

            var sqsQueue1 = TestQueue(() =>
                GetErrorMessages(() => Interlocked.Increment(ref messagesRequested)));

            var queues = new List<ISqsQueue> { sqsQueue1 };
            IMessageDispatcher dispatcher =
                new FakeDispatcher(() => Interlocked.Increment(ref messagesDispatched));

            var defaults = new SubscriptionGroupSettingsBuilder()
                .WithDefaultConcurrencyLimit(8);
            var settings = new Dictionary<string, SubscriptionGroupConfigBuilder>
            {
                { "test", new SubscriptionGroupConfigBuilder("test").AddQueues(queues) },
            };

            var subscriptionGroupFactory = new SubscriptionGroupFactory(
                dispatcher,
                MessageMonitor,
                LoggerFactory);

            ISubscriptionGroup collection = subscriptionGroupFactory.Create(defaults, settings);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Act
            var runTask = collection.RunAsync(cts.Token);

            await Patiently.AssertThatAsync(_outputHelper,
                () =>
                {
                    messagesRequested.ShouldBeGreaterThan(1);
                    messagesDispatched.ShouldBe(0);
                });

        }

        [Fact]
        public async Task Message_Processing_Throwing_Exceptions_Continues_To_Request_Messages()
        {
            // Arrange
            int messagesRequested = 0;
            int messagesDispatched = 0;

            var sqsQueue1 = TestQueue(() => GetErrorMessages(() => messagesRequested++));

            var queues = new List<ISqsQueue> { sqsQueue1 };
            IMessageDispatcher dispatcher =
                new FakeDispatcher(() => Interlocked.Increment(ref messagesDispatched));

            var defaults = new SubscriptionGroupSettingsBuilder()
                .WithDefaultConcurrencyLimit(1);
            var settings = new Dictionary<string, SubscriptionGroupConfigBuilder>
            {
                { "test", new SubscriptionGroupConfigBuilder("test").AddQueues(queues) },
            };

            var subscriptionGroupFactory = new SubscriptionGroupFactory(
                dispatcher,
                MessageMonitor,
                LoggerFactory);

            ISubscriptionGroup collection = subscriptionGroupFactory.Create(defaults, settings);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Act
            var runTask = collection.RunAsync(cts.Token);

            await Patiently.AssertThatAsync(_outputHelper,
                () =>
                {
                    messagesRequested.ShouldBeGreaterThan(1);
                    messagesDispatched.ShouldBe(0);
                });

            await Assert.ThrowsAsync<OperationCanceledException>(() => runTask);
        }

        private static IEnumerable<ReceiveMessageResponse> GetErrorMessages(Action onMessageRequested)
        {
            onMessageRequested();
            throw new Exception();
        }

        private static ISqsQueue TestQueue(Func<IEnumerable<ReceiveMessageResponse>> getMessages)
        {
            var fakeSqs = new FakeAmazonSqs(getMessages);
            var fakeQueue =
                new FakeSqsQueue("test-queue", "fake-region", new Uri("http://test.com"), fakeSqs);

            return fakeQueue;
        }
    }
}
