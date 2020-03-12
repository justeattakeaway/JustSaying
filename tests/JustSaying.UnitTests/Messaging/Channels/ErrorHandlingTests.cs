using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Monitoring;
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

        protected static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMilliseconds(100);

        public ErrorHandlingTests(ITestOutputHelper testOutputHelper)
        {
            this.LoggerFactory = testOutputHelper.ToLoggerFactory();
        }

        [Fact]
        public async Task Sqs_Client_Throwing_Exceptions_Continues_To_Request_Messages()
        {
            // Arrange
            int messagesDispatched = 0;

            var sqsQueue1 = TestQueue(GetErrorMessages);

            var queues = new List<ISqsQueue> { sqsQueue1 };
            IMessageDispatcher dispatcher = TestDispatcher(() => Interlocked.Increment(ref messagesDispatched));

            var config = new ConsumerConfig();
            config.WithDefaultSqsPolicy(LoggerFactory);

            var bus = new ConsumerBus(
                queues,
                config,
                dispatcher,
                Substitute.For<IMessageMonitor>(),
                LoggerFactory);

            var cts = new CancellationTokenSource();

            // Act
            var runTask = bus.Run(cts.Token);

            cts.CancelAfter(TimeoutPeriod);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

            // Assert
            messagesDispatched.ShouldBe(0);
        }

        private static Task<IList<Message>> GetErrorMessages()
        {
            throw new InvalidOperationException();
        }

        private static ISqsQueue TestQueue(Func<Task<IList<Message>>> getMessages)
        {
            ISqsQueue sqsQueueMock = Substitute.For<ISqsQueue>();
            sqsQueueMock
                .GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(async _ => await getMessages());

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
