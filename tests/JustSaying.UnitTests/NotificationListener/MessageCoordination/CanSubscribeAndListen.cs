using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.NotificationListener.MessageCoordination
{
    public class CanSubscribeAndListen
    {
        private readonly ITestOutputHelper _outputHelper;

        public CanSubscribeAndListen(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task CanListen_StopsListeningWhenTokenCancelled()
        {
            var coordinator = CreateMessageCoordinator(_outputHelper.ToLoggerFactory());
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var listening = false;
            _ = Task.Run(async () =>
            {
                await coordinator.ListenAsync(cts.Token).ConfigureAwait(false);
                listening = false;
            });
            listening = true;

            Assert.True(listening);

            await Task.Delay(2000);

            Assert.False(listening);
        }

        [Fact]
        public async Task MessageResponseIsValid_MessageIsHandled()
        {
            // Arrange
            var receiptHandle = "ReceiptHandle";
            var messageBody = "Body";
            var messageReceiver = CreateMessageReceiver(receiptHandle, messageBody);
            var messageDispatcher = Substitute.For<IMessageDispatcher>();

            var coordinator = CreateMessageCoordinator(_outputHelper.ToLoggerFactory(), messageReceiver, messageDispatcher);

            // Act
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            _ = coordinator.ListenAsync(cts.Token);

            await Task.Delay(500);

            // Assert
            await messageDispatcher.Received()
                .DispatchMessage(
                    Arg.Is<Amazon.SQS.Model.Message>(
                        msg => msg.ReceiptHandle == receiptHandle && msg.Body == messageBody),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task MessageResponseNull_ContinuesToRequestMessages()
        {
            // Arrange
            var messageDispatcher = Substitute.For<IMessageDispatcher>();
            var messageReceiver = Substitute.For<IMessageReceiver>();
            messageReceiver.GetMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(_ => default(Amazon.SQS.Model.ReceiveMessageResponse));
            var coordinator = CreateMessageCoordinator(_outputHelper.ToLoggerFactory(), messageReceiver, messageDispatcher);

            // Act
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            _ = Task.Run(() => coordinator.ListenAsync(cts.Token));

            await Task.Delay(500);

            // Assert
            await messageReceiver.Received()
                 .GetMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
            messageReceiver.ClearReceivedCalls();
            await Task.Delay(500);
            await messageReceiver.Received()
                 .GetMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());

            await messageDispatcher.DidNotReceive()
                 .DispatchMessage(Arg.Any<Amazon.SQS.Model.Message>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessingStrategyBlocks_LoopStopsRequestingMessages()
        {
            // Arrange
            var messageReceiver = Substitute.For<IMessageReceiver>();
            var completionSource = new TaskCompletionSource<bool>();
            var messageProcessingStrategy = CreateMessageProcessingStrategy(() => completionSource.Task);
            var coordinator = CreateMessageCoordinator(
                _outputHelper.ToLoggerFactory(),
                messageReceiver,
                messageProcessingStrategy: messageProcessingStrategy);

            // Act
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            _ = Task.Run(() => coordinator.ListenAsync(cts.Token));

            await Task.Delay(500);

            // Assert
            await messageReceiver.Received(1)
                 .GetMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());

            messageReceiver.ClearReceivedCalls();
            completionSource.SetResult(true);

            await messageReceiver.Received()
                 .GetMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task MessageDispatcherBlocks_LoopStopsRequestingMessages()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<bool>();
            var messageReceiver = CreateMessageReceiver("receiptHandle", "messageBody");
            var messageDispatcher = Substitute.For<IMessageDispatcher>();
            messageDispatcher.DispatchMessage(Arg.Any<Amazon.SQS.Model.Message>(), Arg.Any<CancellationToken>())
                .Returns(_ => completionSource.Task);
            var messageProcessingStrategy = new TestMessageProcessingStrategy();

            var coordinator = CreateMessageCoordinator(
                _outputHelper.ToLoggerFactory(),
                messageReceiver,
                messageDispatcher,
                messageProcessingStrategy);

            // Act
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            _ = Task.Run(() => coordinator.ListenAsync(cts.Token));

            await Task.Delay(500);

            // Assert
            await messageReceiver.Received(1)
                 .GetMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
            await messageDispatcher.Received(1)
                 .DispatchMessage(Arg.Any<Amazon.SQS.Model.Message>(), Arg.Any<CancellationToken>());

            messageReceiver.ClearReceivedCalls();
            completionSource.SetResult(true);

            await messageReceiver.Received()
                 .GetMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
            await messageDispatcher.Received()
                .DispatchMessage(Arg.Any<Amazon.SQS.Model.Message>(), Arg.Any<CancellationToken>());
        }

        private static IMessageCoordinator CreateMessageCoordinator(
            ILoggerFactory loggerFactory,
            IMessageReceiver messageReceiver = null,
            IMessageDispatcher messageDispatcher = null,
            IMessageProcessingStrategy messageProcessingStrategy = null)
        {
            var logger = loggerFactory.CreateLogger("test-logger");
            messageReceiver ??= Substitute.For<IMessageReceiver>();
            messageDispatcher ??= Substitute.For<IMessageDispatcher>();

            var messageMonitor = Substitute.For<IMessageMonitor>();
            messageProcessingStrategy ??= new DefaultThrottledThroughput(messageMonitor, logger);

            return new MessageCoordinator(
                logger,
                messageReceiver,
                messageDispatcher,
                messageProcessingStrategy);
        }

        private static IMessageReceiver CreateMessageReceiver(string receiptHandle, string messageBody)
        {
            var messageReceiver = Substitute.For<IMessageReceiver>();
            messageReceiver.GetMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new Amazon.SQS.Model.ReceiveMessageResponse
                {
                    Messages = new List<Amazon.SQS.Model.Message>
                    {
                        new Amazon.SQS.Model.Message { ReceiptHandle = receiptHandle, Body = messageBody },
                    }
                });

            return messageReceiver;
        }

        private static IMessageProcessingStrategy CreateMessageProcessingStrategy(
            Func<Task> waitForThrottlingAsync)
        {
            var messageProcessingStrategy = Substitute.For<IMessageProcessingStrategy>();
            messageProcessingStrategy.WaitForAvailableWorkerAsync()
                .Returns(1);
            messageProcessingStrategy.WaitForThrottlingAsync(Arg.Any<bool>())
                .Returns(ci => waitForThrottlingAsync());

            return messageProcessingStrategy;
        }
    }
}
