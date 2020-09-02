using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels
{
    public class SubscriptionGroupCollectionTests
    {
        private ILoggerFactory LoggerFactory { get; }
        private IMessageMonitor MessageMonitor { get; }

        public SubscriptionGroupCollectionTests(ITestOutputHelper testOutputHelper)
        {
            LoggerFactory = testOutputHelper.ToLoggerFactory();
            MessageMonitor = new LoggingMonitor(LoggerFactory.CreateLogger<IMessageMonitor>());
        }

        private static readonly TimeSpan TimeoutPeriod = TimeSpan.FromSeconds(1);

        [Fact]
        public async Task Add_Different_Handler_Per_Queue()
        {
            // Arrange
            string group1 = "group1";
            string group2 = "group2";
            string queueName1 = "queue1";
            string queueName2 = "queue2";

            JustSaying.JustSayingBus bus = CreateBus();

            var handler1 = new InspectableHandler<TestJustSayingMessage>();
            var handler2 = new InspectableHandler<TestJustSayingMessage>();

            bus.AddMessageHandler(queueName1, () => handler1);
            bus.AddMessageHandler(queueName2, () => handler2);

            ISqsQueue queue1 = TestQueue(bus.SerializationRegister, queueName1);
            ISqsQueue queue2 = TestQueue(bus.SerializationRegister, queueName2);

            bus.AddQueue(group1, queue1);
            bus.AddQueue(group2, queue2);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            // Act
            await bus.StartAsync(cts.Token);
            await bus.Completion;

            // Assert
            handler1.ReceivedMessages.Count.ShouldBeGreaterThan(0);
            foreach (var message in handler1.ReceivedMessages)
            {
                message.QueueName.ShouldBe(queueName1);
            }

            handler2.ReceivedMessages.Count.ShouldBeGreaterThan(0);
            foreach (var message in handler2.ReceivedMessages)
            {
                message.QueueName.ShouldBe(queueName2);
            }

            bus.Dispose();
        }

        private JustSaying.JustSayingBus CreateBus()
        {
            var config = Substitute.For<IMessagingConfig>();
            var serializationRegister = new MessageSerializationRegister(
                new NonGenericMessageSubjectProvider(),
                new NewtonsoftSerializationFactory());

            var bus = new JustSaying.JustSayingBus(config, serializationRegister, LoggerFactory)
            {
                Monitor = MessageMonitor,
            };

            var defaultSubscriptionSettings = new SubscriptionGroupSettingsBuilder()
                .WithDefaultMultiplexerCapacity(1)
                .WithDefaultPrefetch(1)
                .WithDefaultBufferSize(1)
                .WithDefaultConcurrencyLimit(1);

            bus.SetGroupSettings(defaultSubscriptionSettings, new Dictionary<string, SubscriptionGroupConfigBuilder>());

            return bus;
        }

        private static ISqsQueue TestQueue(
            IMessageSerializationRegister messageSerializationRegister,
            string queueName,
            Action spy = null)
        {
            ReceiveMessageResponse GetMessages()
            {
                Thread.Sleep(30);
                spy?.Invoke();
                var message = new TestJustSayingMessage
                {
                    QueueName = queueName,
                };

                var messages = new List<Message>
                {
                    new TestMessage { Body = messageSerializationRegister.Serialize(message, false) },
                };

                return new ReceiveMessageResponse { Messages = messages };
            }

            IAmazonSQS sqsClientMock = Substitute.For<IAmazonSQS>();
            sqsClientMock
                .ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
                .Returns(_ => GetMessages());

            ISqsQueue sqsQueueMock = Substitute.For<ISqsQueue>();
            sqsQueueMock.Uri.Returns(new Uri("http://test.com"));
            sqsQueueMock.Client.Returns(sqsClientMock);
            sqsQueueMock.QueueName.Returns(queueName);
            sqsQueueMock.Uri.Returns(new Uri("http://foo.com"));

            return sqsQueueMock;
        }

        private class TestMessage : Message
        {
            public override string ToString()
            {
                return Body;
            }
        }

        private class TestJustSayingMessage : JustSaying.Models.Message
        {
            public string QueueName { get; set; }

            public override string ToString()
            {
                return QueueName;
            }
        }
    }
}
