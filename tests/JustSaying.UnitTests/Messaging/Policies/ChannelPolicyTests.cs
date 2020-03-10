using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Monitoring;
using JustSaying.UnitTests.Messaging.Policies.ExamplePolicies;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Policies
{
    public class ChannelPolicyTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ChannelPolicyTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMilliseconds(100);

        [Fact]
        public async Task ErrorHandlingAroundSqs()
        {
            // Arrange
            int queueCalledCount = 0;
            int dispatchedMessageCount = 0;
            var sqsQueue = TestQueue(() => Interlocked.Increment(ref queueCalledCount));

            var queues = new List<ISqsQueue> { sqsQueue };

            var config = new ConsumerConfig();
            config.WithSqsPolicy(
               next => new ErrorHandlingMiddleware<GetMessagesContext, IList<Message>, InvalidOperationException>(next));

            IMessageDispatcher dispatcher = TestDispatcher(() => Interlocked.Increment(ref dispatchedMessageCount));

            var bus = new ConsumerBus(
                queues,
                config,
                dispatcher,
                Substitute.For<IMessageMonitor>(),
                _testOutputHelper.ToLoggerFactory());

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            // Act and Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bus.Run(cts.Token));

            queueCalledCount.ShouldBeGreaterThan(1);
            dispatchedMessageCount.ShouldBe(0);
        }

        private static ISqsQueue TestQueue(Action spy = null)
        {
            async Task<IList<Message>> GetMessages()
            {
                await Task.Delay(5).ConfigureAwait(false);
                spy?.Invoke();
                throw new InvalidOperationException();
            }

            ISqsQueue sqsQueueMock = Substitute.For<ISqsQueue>();
            sqsQueueMock
                .GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(async _ => await GetMessages());

            return sqsQueueMock;
        }

        private static IMessageDispatcher TestDispatcher(Action spy = null)
        {
            async Task OnDispatchMessage()
            {
                await Task.Delay(5).ConfigureAwait(false);
                spy?.Invoke();
            }

            IMessageDispatcher dispatcherMock = Substitute.For<IMessageDispatcher>();
            dispatcherMock
                .DispatchMessageAsync(Arg.Any<IQueueMessageContext>(), Arg.Any<CancellationToken>())
                .Returns(async _ => await OnDispatchMessage());

            return dispatcherMock;
        }
    }
}
