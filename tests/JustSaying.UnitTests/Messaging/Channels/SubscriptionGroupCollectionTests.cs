using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
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

        private static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMilliseconds(100);

        [Fact]
        public async Task Add_Different_Handler_Per_Queue()
        {
            // Arrange
            string group1 = "group1";
            string group2 = "group2";
            string region = "region";
            string queueName1 = "queue1";
            string queueName2 = "queue2";

            JustSaying.JustSayingBus bus = CreateBus();

            ISqsQueue queue1 = TestQueue(bus.SerializationRegister, queueName1);
            ISqsQueue queue2 = TestQueue(bus.SerializationRegister, queueName2);

            bus.AddQueue(region, group1, queue1);
            bus.AddQueue(region, group2, queue2);

            var handledBy1 = new List<TestJustSayingMessage>();
            var handledBy2 = new List<TestJustSayingMessage>();

            bus.AddMessageHandler(queueName1, () => new TestHandler<TestJustSayingMessage>(x => handledBy1.Add(x)));
            bus.AddMessageHandler(queueName2, () => new TestHandler<TestJustSayingMessage>(x => handledBy2.Add(x)));

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeoutPeriod);

            // Act
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bus.Start(cts.Token));

            // Assert
            handledBy1.Count.ShouldBeGreaterThan(0);
            foreach (var message in handledBy1)
            {
                message.QueueName.ShouldBe(queueName1);
            }

            handledBy2.Count.ShouldBeGreaterThan(0);
            foreach (var message in handledBy2)
            {
                message.QueueName.ShouldBe(queueName2);
            }
        }

        private JustSaying.JustSayingBus CreateBus()
        {
            var config = Substitute.For<IMessagingConfig>();
            config.SubscriptionConfig.Returns(new SubscriptionConfig());
            var serializationRegister = new MessageSerializationRegister(
                new NonGenericMessageSubjectProvider(),
                new NewtonsoftSerializationFactory());
            var serializationFactory = new NewtonsoftSerializationFactory();

            var bus = new JustSaying.JustSayingBus(config, serializationRegister, LoggerFactory)
            {
                Monitor = MessageMonitor,
            };

            bus.SerializationRegister.AddSerializer<TestJustSayingMessage>();

            return bus;
        }

        private static ISqsQueue TestQueue(
            IMessageSerializationRegister messageSerializationRegister,
            string queueName,
            Action spy = null)
        {
            IList<Amazon.SQS.Model.Message> GetMessages()
            {
                spy?.Invoke();
                var message = new TestJustSayingMessage
                {
                    QueueName = queueName,
                };

                return new List<Amazon.SQS.Model.Message>
                {
                    new TestMessage { Body = messageSerializationRegister.Serialize(message, false) },
                };
            }

            ISqsQueue sqsQueueMock = Substitute.For<ISqsQueue>();
            sqsQueueMock
                .GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(_ => GetMessages());
            sqsQueueMock
                .QueueName
                .Returns(queueName);
            sqsQueueMock
                .Uri
                .Returns(new Uri("http://foo.com"));

            return sqsQueueMock;
        }

        private ISubscriptionGroupCollection CreateSubscriptionGroup(
            IList<ISqsQueue> queues,
            IMessageDispatcher dispatcher)
        {
            var config = new SubscriptionConfig();

            var settings = new Dictionary<string, SubscriptionGroupSettingsBuilder>
            {
                { "test",  new SubscriptionGroupSettingsBuilder("test").WithDefaultsFrom(config).AddQueues(queues) },
            };

            var consumerGroupFactory = new SubscriptionGroupFactory(
                config,
                dispatcher,
                MessageMonitor,
                LoggerFactory);

            return consumerGroupFactory.Create(settings);
        }

        private class TestMessage : Amazon.SQS.Model.Message
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
