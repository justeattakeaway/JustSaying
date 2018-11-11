using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
{
    public abstract class BaseQueuePollingTest : XAsyncBehaviourTest<JustSaying.AwsTools.MessageHandling.SqsNotificationListener>
    {
        protected const string QueueUrl = "http://testurl.com/queue";
        protected IAmazonSQS Sqs;
        protected SimpleMessage DeserialisedMessage;
        protected const string MessageBody = "object";
        protected IHandlerAsync<SimpleMessage> Handler;
        protected IMessageMonitor Monitor;
        protected ILoggerFactory LoggerFactory;
        protected IMessageSerialisationRegister SerialisationRegister;
        protected IMessageLockAsync MessageLock;
        protected readonly string MessageTypeString = typeof(SimpleMessage).ToString();

        protected override Task<JustSaying.AwsTools.MessageHandling.SqsNotificationListener> CreateSystemUnderTestAsync()
        {
            var queue = new SqsQueueByUrl(RegionEndpoint.EUWest1, new Uri(QueueUrl), Sqs);
            return Task.FromResult(new JustSaying.AwsTools.MessageHandling.SqsNotificationListener(queue, SerialisationRegister, Monitor, LoggerFactory, null, MessageLock));
        }

        protected override Task Given()
        {
            LoggerFactory = new LoggerFactory();
            Sqs = Substitute.For<IAmazonSQS>();
            SerialisationRegister = Substitute.For<IMessageSerialisationRegister>();
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

            DeserialisedMessage = new SimpleMessage { RaisingComponent = "Component" };
            SerialisationRegister.DeserializeMessage(Arg.Any<string>()).Returns(DeserialisedMessage);
            return Task.CompletedTask;
        }
        
        protected override async Task When()
        {
            var doneSignal = new TaskCompletionSource<object>();
            var signallingHandler = new SignallingHandler<SimpleMessage>(doneSignal, Handler);

            SystemUnderTest.AddMessageHandler(() => signallingHandler);
            var cts = new CancellationTokenSource();
            SystemUnderTest.Listen(cts.Token);

            // wait until it's done
            var doneOk = await Tasks.WaitWithTimeoutAsync(doneSignal.Task);

            cts.Cancel();

            doneOk.ShouldBeTrue("Timeout occured before done signal");
        }

        protected ReceiveMessageResponse GenerateResponseMessage(string messageType, Guid messageId)
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

        protected string SqsMessageBody(string messageType)
        {
            return "{\"Subject\":\"" + messageType + "\"," + "\"Message\":\"" + MessageBody + "\"}";
        }
    }
}
