using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Channels.Factory;
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
            int messagesDispatched = 0;

            var sqsQueue1 = TestQueue(GetErrorMessages);

            var queues = new List<ISqsQueue> { sqsQueue1 };
            IMessageDispatcher dispatcher = new FakeDispatcher(() => Interlocked.Increment(ref messagesDispatched));

            var config = new ConsumerConfig();
            config.WithDefaultSqsPolicy(LoggerFactory);

            var receiveBufferFactory = new ReceiveBufferFactory(LoggerFactory, config, MessageMonitor);
            var multiplexerFactory = new MultiplexerFactory(LoggerFactory);
            var consumerFactory = new ConsumerFactory(dispatcher);
            var consumerBusFactory = new SingleConsumerBusFactory(config,
                queues, multiplexerFactory, receiveBufferFactory, consumerFactory, LoggerFactory);

            var bus = new MultipleConsumerBus(
                consumerBusFactory, LoggerFactory.CreateLogger<MultipleConsumerBus>(), config);

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
    }
}
