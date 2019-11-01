using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.NotificationListener.MessageProcessing
{
    public class ProcessesMessages
    {
        private readonly ITestOutputHelper _outputHelper;

        public ProcessesMessages(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public async Task MessageResponseIsValid_MessageIsHandledAndRemovedFromQueue(bool handlerSuccess, int deleteCount)
        {
            // Arrange
            var myProperty = "hello-just-saying";
            var message = new TestMessage { MyProperty = myProperty };
            var serializationRegister = CreateSerializationRegister(message);
            var sqsmessage = new Amazon.SQS.Model.Message { ReceiptHandle = "hello", Body = "Not testing this" };
            var sqsClient = Substitute.For<IAmazonSQS>();
            var handler = CreateHandler<TestMessage>(handlerSuccess);

            var dispatcher = CreateMessageDispatcher(_outputHelper.ToLoggerFactory(), sqsClient, serializationRegister);
            dispatcher.AddMessageHandler<TestMessage>(() => handler);

            // Act
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await dispatcher.DispatchMessage(sqsmessage, cts.Token);

            await Task.Delay(500);

            // Assert
            await handler.Received()
                .Handle(Arg.Is<TestMessage>(msg => msg.MyProperty == myProperty));

            await sqsClient.Received(deleteCount)
                 .DeleteMessageAsync(Arg.Is<Amazon.SQS.Model.DeleteMessageRequest>(r => r.ReceiptHandle == "hello"));
        }

        [Fact]
        public async Task MessageResponseCannotBeDeserialized_MessageIsRemovedFromQueue()
        {
            // Arrange
            var serializationRegister = CreateSerializationRegisterWithException();
            var message = new Amazon.SQS.Model.Message { ReceiptHandle = "hello", Body = "Not testing this" };
            var sqsClient = Substitute.For<IAmazonSQS>();
            var dispatcher = CreateMessageDispatcher(_outputHelper.ToLoggerFactory(), sqsClient, serializationRegister);

            // Act
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await dispatcher.DispatchMessage(message, cts.Token);

            await Task.Delay(500);

            // Assert
            await sqsClient.Received()
                 .DeleteMessageAsync(Arg.Is<Amazon.SQS.Model.DeleteMessageRequest>(r => r.ReceiptHandle == "hello"));
        }

        private static IMessageDispatcher CreateMessageDispatcher(
            ILoggerFactory loggerFactory,
            IAmazonSQS sqsClient,
            IMessageSerializationRegister messageSerializationRegister = null)
        {
            sqsClient ??= Substitute.For<IAmazonSQS>();
            messageSerializationRegister ??= Substitute.For<IMessageSerializationRegister>();

            var queue = Substitute.For<ISqsQueue>();
            queue.Region.Returns(RegionEndpoint.EUWest2);
            queue.QueueName.Returns("test-queue");
            queue.Uri.Returns(new Uri("http://localhost"));
            queue.Client.Returns(sqsClient);

            var messageDispatcher = new MessageDispatcher(
                queue,
                messageSerializationRegister,
                Substitute.For<IMessageMonitor>(),
                Substitute.For<Action<Exception, Amazon.SQS.Model.Message>>(),
                loggerFactory,
                Substitute.For<IMessageBackoffStrategy>(),
                Substitute.For<IMessageContextAccessor>(),
                Substitute.For<IMessageLockAsync>());

            return messageDispatcher;
        }

        public class TestMessage : Models.Message
        {
            public string MyProperty { get; set; }
        }

        private static IHandlerAsync<T> CreateHandler<T>(bool returns) where T : Models.Message
        {
            var handler = Substitute.For<IHandlerAsync<T>>();
            handler.Handle(Arg.Any<T>()).Returns(returns);

            return handler;
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
    }
}
