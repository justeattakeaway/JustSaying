using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
{
    // this class should be disposable because it owns a ILoggerFactory and IAmazonSQS,
    // but that's not important for tests.
    // it's handled by IAsyncLifetime
#pragma warning disable CA1001
    public abstract class BaseQueuePollingTest : IAsyncLifetime
#pragma warning restore CA1001
    {
        protected const string QueueUrl = "http://testurl.com/queue";
        protected IAmazonSQS Sqs;
        protected SimpleMessage DeserializedMessage;
        protected const string MessageBody = "object";
        protected IHandlerAsync<SimpleMessage> Handler;
        protected IMessageMonitor Monitor;
        protected ILoggerFactory LoggerFactory;
        protected IMessageSerializationRegister SerializationRegister;
        protected IMessageLockAsync MessageLock;
        protected readonly string MessageTypeString = typeof(SimpleMessage).ToString();

        protected JustSaying.AwsTools.MessageHandling.SqsNotificationListener SystemUnderTest { get; private set; }

        public virtual async Task InitializeAsync()
        {
            Given();

            SystemUnderTest = CreateSystemUnderTest();

            await WhenAsync().ConfigureAwait(false);
        }

        public virtual Task DisposeAsync()
        {
            if (LoggerFactory != null)
            {
                LoggerFactory.Dispose();
                LoggerFactory = null;
            }

            if (Sqs != null)
            {
                Sqs.Dispose();
                Sqs = null;
            }

            return Task.CompletedTask;
        }

        protected JustSaying.AwsTools.MessageHandling.SqsNotificationListener CreateSystemUnderTest()
        {
            var queue = new SqsQueueByUrl(RegionEndpoint.EUWest1, new Uri(QueueUrl), Sqs);
            var listener = new JustSaying.AwsTools.MessageHandling.SqsNotificationListener(
                queue, SerializationRegister, Monitor, LoggerFactory,
                Substitute.For<IMessageContextAccessor>(),
                null, MessageLock);
            return listener;
        }

        protected virtual void Given()
        {
            LoggerFactory = new LoggerFactory();
            Sqs = Substitute.For<IAmazonSQS>();
            SerializationRegister = Substitute.For<IMessageSerializationRegister>();
            Monitor = Substitute.For<IMessageMonitor>();
            Handler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            LoggerFactory = Substitute.For<ILoggerFactory>();

            var response = GenerateResponseMessage(MessageTypeString, Guid.NewGuid());

            Sqs.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(
                    x => Task.FromResult(response),
                    x => Task.FromResult(new ReceiveMessageResponse()));

            DeserializedMessage = new SimpleMessage { RaisingComponent = "Component" };
            SerializationRegister.DeserializeMessage(Arg.Any<string>()).Returns(DeserializedMessage);
        }

        protected virtual async Task WhenAsync()
        {
            var doneSignal = new TaskCompletionSource<object>();
            var signallingHandler = new SignallingHandler<SimpleMessage>(doneSignal, Handler);

            SystemUnderTest.AddMessageHandler(() => signallingHandler);
            var cts = new CancellationTokenSource();
            SystemUnderTest.Listen(cts.Token);

            // wait until it's done
            var doneOk = await TaskHelpers.WaitWithTimeoutAsync(doneSignal.Task);

            cts.Cancel();

            doneOk.ShouldBeTrue("Timeout occured before done signal");
        }

        protected static ReceiveMessageResponse GenerateResponseMessage(string messageType, Guid messageId)
        {
            return new ReceiveMessageResponse
            {
                Messages = new List<Message>
                {
                    new Message
                    {
                        MessageId = messageId.ToString(),
                        Body = SqsMessageBody(messageType)
                    },
                    new Message
                    {
                        MessageId = messageId.ToString(),
                        Body = "{\"Subject\":\"SOME_UNKNOWN_MESSAGE\"," + "\"Message\":\"SOME_RANDOM_MESSAGE\"}"
                    }
                }
            };
        }

        protected static string SqsMessageBody(string messageType)
        {
            return "{\"Subject\":\"" + messageType + "\"," + "\"Message\":\"" + MessageBody + "\"}";
        }
    }
}
