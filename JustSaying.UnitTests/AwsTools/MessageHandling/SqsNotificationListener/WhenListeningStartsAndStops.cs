using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageProcessingStrategies;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
{
    public class WhenListeningStartsAndStops : BaseQueuePollingTest
    {
        private const string SubjectOfMessageAfterStop = @"POST_STOP_MESSAGE";
        private const string BodyOfMessageAfterStop = @"{""Subject"":""POST_STOP_MESSAGE"",""Message"":""object""}";

        private int expectedMaxMessageCount;

        protected override async Task Given()
        {
            await base.Given();

            // we expect to get max 10 messages per batch
            // except on single-core machines when we top out at ParallelHandlerExecutionPerCore=8
            expectedMaxMessageCount = Math.Min(MessageConstants.MaxAmazonMessageCap, 
                Environment.ProcessorCount * MessageConstants.ParallelHandlerExecutionPerCore);

            var response1 = GenerateResponseMessage(SubjectOfMessageAfterStop, Guid.NewGuid());
            var response2 = new ReceiveMessageResponse
            {
                Messages = new List<Message>()
            };

            Sqs.ReceiveMessageAsync(
                Arg.Any<ReceiveMessageRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => response1,
                _ => response2);
        }

        protected override async Task When()
        {
            await base.When();

            var cts = new CancellationTokenSource();
            SystemUnderTest.Listen(cts.Token);
            await Task.Yield();

            cts.Cancel();
            await Task.Yield();
        }

        [Fact]
        public void MessagesAreReceived()
        {
            Sqs.Received().ReceiveMessageAsync(
                Arg.Any<ReceiveMessageRequest>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public void CorrectQueueIsPolled()
        {
            Sqs.Received().ReceiveMessageAsync(
                Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == QueueUrl),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public void TheMaxMessageAllowanceIsGrabbed()
        {
            Sqs.Received().ReceiveMessageAsync(
                Arg.Is<ReceiveMessageRequest>(x => x.MaxNumberOfMessages == expectedMaxMessageCount),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public void MessageIsProcessed()
        {
            SerialisationRegister.Received().DeserializeMessage(
                BodyOfMessageAfterStop);
        }
    }
}
