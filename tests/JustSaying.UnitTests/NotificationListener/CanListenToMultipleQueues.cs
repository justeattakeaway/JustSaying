using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;
using static JustSaying.UnitTests.NotificationListener.CanSubscribeAndListen;

namespace JustSaying.UnitTests.NotificationListener
{
    public class CanCreateMultipleListeners
    {
        private readonly ITestOutputHelper _outputHelper;

        public CanCreateMultipleListeners(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void CanListen_StopsListeningWhenTokenCancelled()
        {
            // Arrange
            var sqsClient1 = CreateSqsClient();
            var sqsClient2 = CreateSqsClient();
            var listeners = CreateListeners(_outputHelper.ToLoggerFactory(), new[] { sqsClient1, sqsClient2 });
            Assert.Equal(2, listeners.Count);

            // Act
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            foreach (var listener in listeners)
            {
                listener.Listen(cts.Token);
            }

            // Assert
            foreach (var listener in listeners)
            {
                Assert.True(listener.IsListening);
            }
        }

        [Fact]
        public async Task OneQueueThrowingErrors_OtherContinuesToProcessMessages()
        {
            // Arrange
            var myProperty = "hello-just-saying";
            var message = new TestMessage { MyProperty = myProperty };
            var serializationRegister = CreateSerializationRegister(message);

            var sqsClient1 = CreateSqsClient();
            var sqsClient2 = CreateSqsClient();
            var listeners = CreateListeners(
                _outputHelper.ToLoggerFactory(), new[] { sqsClient1, sqsClient2 }, serializationRegister);
            Assert.Equal(2, listeners.Count);

            var successfulHandler = CreateHandler<TestMessage>(true);
            var handlerWithException = CreateHandlerWithException<TestMessage>();

            var listener1 = listeners[0];
            var listener2 = listeners[1];

            listener1.AddMessageHandler<TestMessage>(() => successfulHandler);
            listener2.AddMessageHandler<TestMessage>(() => handlerWithException);

            // Act
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            foreach (var listener in listeners)
            {
                listener.Listen(cts.Token);
            }

            // Assert
            await successfulHandler.Received()
                .Handle(Arg.Is<TestMessage>(msg => msg.MyProperty == myProperty));

            await sqsClient1.Received()
                 .DeleteMessageAsync(Arg.Is<Amazon.SQS.Model.DeleteMessageRequest>(r => r.ReceiptHandle == "hello"));

            await handlerWithException.Received()
                .Handle(Arg.Is<TestMessage>(msg => msg.MyProperty == myProperty));

            await sqsClient2.DidNotReceive()
                 .DeleteMessageAsync(Arg.Is<Amazon.SQS.Model.DeleteMessageRequest>(r => r.ReceiptHandle == "hello"));
        }

        private static List<INotificationSubscriber> CreateListeners(
            ILoggerFactory loggerFactory,
            IEnumerable<IAmazonSQS> sqsClients,
            IMessageSerializationRegister messageSerializationRegister = null)
        {
            loggerFactory ??= Substitute.For<ILoggerFactory>();
            messageSerializationRegister ??= CreateSerializationRegister();
            var messageMonitor = Substitute.For<IMessageMonitor>();
            var messageContextAccessor = Substitute.For<IMessageContextAccessor>();
            var messageProcessingStrategy = new TestMessageProcessingStrategy();
            var onError = Substitute.For<Action<Exception, Amazon.SQS.Model.Message>>();
            var messageLockAsync = Substitute.For<IMessageLockAsync>();
            var messageBackoffStrategy = Substitute.For<IMessageBackoffStrategy>();

            var listeners = new List<INotificationSubscriber>();
            foreach (var sqsClient in sqsClients)
            {
                var queue = Substitute.For<ISqsQueue>();
                queue.Region.Returns(RegionEndpoint.EUWest2);
                queue.QueueName.Returns("test-queue");
                queue.Uri.Returns(new Uri("http://localhost"));
                queue.Client.Returns(sqsClient);

                var listener = new SqsNotificationListener(
                   queue,
                   messageSerializationRegister,
                   messageMonitor,
                   loggerFactory,
                   messageContextAccessor,
                   messageProcessingStrategy,
                   onError,
                   messageLockAsync,
                   messageBackoffStrategy);

                listeners.Add(listener);
            }
            return listeners;
        }

        private static IAmazonSQS CreateSqsClient()
        {
            var sqsClient = Substitute.For<IAmazonSQS>();
            sqsClient.ReceiveMessageAsync(Arg.Any<Amazon.SQS.Model.ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(new Amazon.SQS.Model.ReceiveMessageResponse
                {
                    Messages = new List<Amazon.SQS.Model.Message>
                    {
                        new Amazon.SQS.Model.Message { ReceiptHandle = "hello", Body = "Not testing this" },
                    }
                });

            return sqsClient;
        }

        private static IMessageSerializationRegister CreateSerializationRegister(Models.Message message = null)
        {
            var register = Substitute.For<IMessageSerializationRegister>();

            register.DeserializeMessage(Arg.Any<string>())
               .Returns(message);

            return register;
        }

        private static IHandlerAsync<T> CreateHandler<T>(bool returns) where T : Models.Message
        {
            var handler = Substitute.For<IHandlerAsync<T>>();
            handler.Handle(Arg.Any<T>()).Returns(returns);

            return handler;
        }

        private static IHandlerAsync<T> CreateHandlerWithException<T>() where T : Models.Message
        {
            var handler = Substitute.For<IHandlerAsync<T>>();
            handler.Handle(Arg.Any<T>())
                .Returns<Task<bool>>(x => throw new Exception("oh no!"));

            return handler;
        }
    }
}
