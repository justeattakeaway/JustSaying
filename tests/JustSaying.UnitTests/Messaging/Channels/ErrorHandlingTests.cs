using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Monitoring;
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


        protected static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMilliseconds(100);

        public ErrorHandlingTests(ITestOutputHelper testOutputHelper)
        {
            LoggerFactory = testOutputHelper.ToLoggerFactory();
            MessageMonitor = new LoggingMonitor(LoggerFactory.CreateLogger(nameof(IMessageMonitor)));
        }

        [Fact]
        public async Task Sqs_Client_Throwing_Exceptions_Continues_To_Request_Messages()
        {
            // Arrange
            int messagesRequested = 0;
            int messagesDispatched = 0;

            var sqsQueue1 = TestQueue(() => GetErrorMessages(() => messagesRequested++));

            var queues = new List<ISqsQueue> { sqsQueue1 };
            IMessageDispatcher dispatcher = new FakeDispatcher(() => Interlocked.Increment(ref messagesDispatched));

            var defaults = new SubscriptionConfigBuilder()
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

            var cts = new CancellationTokenSource();

            // Act
            var runTask = collection.RunAsync(cts.Token);

            cts.CancelAfter(TimeoutPeriod);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

            // Assert
            messagesRequested.ShouldBeGreaterThan(1);
            messagesDispatched.ShouldBe(0);
        }

        [Fact]
        public async Task Message_Processing_Throwing_Exceptions_Continues_To_Request_Messages()
        {
            // Arrange
            int messagesRequested = 0;
            int messagesDispatched = 0;

            var sqsQueue1 = TestQueue(() => GetErrorMessages(() => messagesRequested++));

            var queues = new List<ISqsQueue> { sqsQueue1 };
            IMessageDispatcher dispatcher = new FakeDispatcher(() => Interlocked.Increment(ref messagesDispatched));

            var defaults = new SubscriptionConfigBuilder()
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

            var cts = new CancellationTokenSource();

            // Act
            var runTask = collection.RunAsync(cts.Token);

            cts.CancelAfter(TimeoutPeriod);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

            // Assert
            messagesRequested.ShouldBeGreaterThan(1);
            messagesDispatched.ShouldBe(0);
        }

        private static Task<IList<Message>> GetErrorMessages(Action onMessageRequested)
        {
            onMessageRequested();
            throw new OperationCanceledException();
        }

        private static ISqsQueue TestQueue(Func<Task<IList<Message>>> getMessages)
        {
            ISqsQueue sqsQueueMock = Substitute.For<ISqsQueue>();
            sqsQueueMock
                .GetMessagesAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(async _ => await getMessages());

            return sqsQueueMock;
        }
    }
}
