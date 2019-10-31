using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.NotificationListener
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
            var listener = CreateListener(_outputHelper.ToLoggerFactory());
            var cts = new CancellationTokenSource(500);
            listener.Listen(cts.Token);
            Assert.True(listener.IsListening);

            await Task.Delay(1000);

            Assert.False(listener.IsListening);
        }

        [Fact]
        public async Task MessageResponseCannotBeDeserialized_MessageIsRemovedFromQueue()
        {
            // Arrange
            var sqsClient = CreateSqsClient();
            var serializationRegister = CreateSerializationRegisterWithException();
            var listener = CreateListener(_outputHelper.ToLoggerFactory(), sqsClient, serializationRegister);

            // Act
            var cts = new CancellationTokenSource(1000);
            listener.Listen(cts.Token);

            await Task.Delay(500);

            // Assert
            await sqsClient.Received()
                 .DeleteMessageAsync(Arg.Is<Amazon.SQS.Model.DeleteMessageRequest>(r => r.ReceiptHandle == "hello"));
        }

        [Fact]
        public async Task MessageResponseIsValid_MessageIsHandledAndRemovedFromQueue()
        {
            // Arrange
            var myProperty = "hello-just-saying";
            var message = new TestMessage { MyProperty = myProperty };
            var sqsClient = CreateSqsClient();
            var serializationRegister = CreateSerializationRegister(message);
            var handler = CreateHandler<TestMessage>(true);

            var listener = CreateListener(_outputHelper.ToLoggerFactory(), sqsClient, serializationRegister);
            listener.AddMessageHandler<TestMessage>(() => handler);

            // Act
            var cts = new CancellationTokenSource(1000);
            listener.Listen(cts.Token);

            await Task.Delay(500);

            // Assert
            await handler.Received()
                .Handle(Arg.Is<TestMessage>(msg => msg.MyProperty == myProperty));

            await sqsClient.Received()
                 .DeleteMessageAsync(Arg.Is<Amazon.SQS.Model.DeleteMessageRequest>(r => r.ReceiptHandle == "hello"));
        }

        [Fact]
        public async Task MessageResponseIsValid_HandlerFails_NotRemovedFromQueue()
        {
            // Arrange
            var myProperty = "hello-just-saying";
            var message = new TestMessage { MyProperty = myProperty };
            var sqsClient = CreateSqsClient();
            var serializationRegister = CreateSerializationRegister(message);
            var handler = CreateHandler<TestMessage>(false);

            var listener = CreateListener(_outputHelper.ToLoggerFactory(), sqsClient, serializationRegister);
            listener.AddMessageHandler<TestMessage>(() => handler);

            // Act
            var cts = new CancellationTokenSource(1000);
            listener.Listen(cts.Token);

            await Task.Delay(500);

            // Assert
            await handler.Received()
                .Handle(Arg.Is<TestMessage>(msg => msg.MyProperty == myProperty));

            await sqsClient.DidNotReceive()
                 .DeleteMessageAsync(Arg.Is<Amazon.SQS.Model.DeleteMessageRequest>(r => r.ReceiptHandle == "hello"));
        }

        [Fact]
        public async Task MessageResponseIsValid_HandlerThrowsError_NotRemovedFromQueue()
        {
            // Arrange
            var myProperty = "hello-just-saying";
            var message = new TestMessage { MyProperty = myProperty };
            var sqsClient = CreateSqsClient();
            var serializationRegister = CreateSerializationRegister(message);
            var handler = CreateHandlerWithException<TestMessage>();

            var listener = CreateListener(_outputHelper.ToLoggerFactory(), sqsClient, serializationRegister);
            listener.AddMessageHandler<TestMessage>(() => handler);

            // Act
            var cts = new CancellationTokenSource(1000);
            listener.Listen(cts.Token);

            await Task.Delay(500);

            // Assert
            await handler.Received()
                .Handle(Arg.Is<TestMessage>(msg => msg.MyProperty == myProperty));

            await sqsClient.DidNotReceive()
                 .DeleteMessageAsync(Arg.Is<Amazon.SQS.Model.DeleteMessageRequest>(r => r.ReceiptHandle == "hello"));
        }

        [Fact]
        public async Task SqsRequestThrowsException_ContinuesToRequestMessages()
        {
            // Arrange
            var myProperty = "hello-just-saying";
            var message = new TestMessage { MyProperty = myProperty };
            var sqsClient = CreateSqsClientWithException();
            var serializationRegister = CreateSerializationRegister(message);
            var listener = CreateListener(_outputHelper.ToLoggerFactory(), sqsClient, serializationRegister);

            // Act
            var cts = new CancellationTokenSource(1000);
            listener.Listen(cts.Token);

            await Task.Delay(500);

            // Assert
            await sqsClient.Received()
                 .ReceiveMessageAsync(Arg.Any<Amazon.SQS.Model.ReceiveMessageRequest>(), Arg.Any<CancellationToken>());
            sqsClient.ClearReceivedCalls();
            await Task.Delay(500);
            await sqsClient.Received()
                 .ReceiveMessageAsync(Arg.Any<Amazon.SQS.Model.ReceiveMessageRequest>(), Arg.Any<CancellationToken>());

            await sqsClient.DidNotReceive()
                 .DeleteMessageAsync(Arg.Is<Amazon.SQS.Model.DeleteMessageRequest>(r => r.ReceiptHandle == "hello"));
        }

        public class TestMessage : Models.Message
        {
            public string MyProperty { get; set; }
        }

        // tests
        // check correct handler is called
        // what happens when handler throws exception
        // what happens if there is no handler
        // what happens if sqs throws exception
        // handled message is deleted from queue
        // - message that cant be deserialised is also deleted

        // ideas for features
        // allow debounce
        // allow circuit break?
        // allow batched processing?
        // allow buffering if sqs throws exceptions

        private static INotificationSubscriber CreateListener(
            ILoggerFactory loggerFactory,
            IAmazonSQS sqsClient = null,
            IMessageSerializationRegister serializationRegister = null)
        {
            sqsClient ??= CreateSqsClient();
            serializationRegister ??= CreateSerializationRegister();
            loggerFactory ??= Substitute.For<ILoggerFactory>();

            var queue = Substitute.For<ISqsQueue>();
            queue.Region.Returns(RegionEndpoint.EUWest2);
            queue.QueueName.Returns("test-queue");
            queue.Uri.Returns(new Uri("http://localhost"));
            queue.Client.Returns(sqsClient);

            return new SqsNotificationListener(
                queue,
                serializationRegister,
                Substitute.For<IMessageMonitor>(),
                Substitute.For<ILoggerFactory>(),
                Substitute.For<IMessageContextAccessor>(),
                Substitute.For<Action<Exception, Amazon.SQS.Model.Message>>(),
                Substitute.For<IMessageLockAsync>(),
                Substitute.For<IMessageBackoffStrategy>());
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

        private static IAmazonSQS CreateSqsClientWithException()
        {
            var sqsClient = Substitute.For<IAmazonSQS>();
            sqsClient.ReceiveMessageAsync(Arg.Any<Amazon.SQS.Model.ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs<Amazon.SQS.Model.ReceiveMessageResponse>(x => throw new Exception("Test exception"));

            return sqsClient;
        }

        private static IMessageSerializationRegister CreateSerializationRegister(Models.Message message = null)
        {
            var register = Substitute.For<IMessageSerializationRegister>();

            register.DeserializeMessage(Arg.Any<string>())
               .Returns(message);

            return register;
        }

        private static IMessageSerializationRegister CreateSerializationRegisterWithException()
        {
            var register = Substitute.For<IMessageSerializationRegister>();

            register.DeserializeMessage(Arg.Any<string>())
                .Returns<Models.Message>(x => throw new MessageFormatNotSupportedException("Test exception"));

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
