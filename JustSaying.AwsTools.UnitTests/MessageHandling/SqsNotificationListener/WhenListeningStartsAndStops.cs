using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustBehave;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenListeningStartsAndStops : BaseQueuePollingTest
    {
        private const string SubjectOfMessageAfterStop = @"POST_STOP_MESSAGE";
        private const string BodyOfMessageAfterStop = @"{""Subject"":""POST_STOP_MESSAGE"",""Message"":""object""}";

        protected override void Given()
        {
            base.Given();

            Sqs.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>())
               .Returns(
                    _ => Task.FromResult(
                        GenerateResponseMessage(SubjectOfMessageAfterStop, Guid.NewGuid())), 
                    _ => Task.FromResult(
                            new ReceiveMessageResponse
                            {
                                Messages = new List<Message>()
                            }));
        }

        protected override async Task When()
        {
            await base.When();

            SystemUnderTest.StopListening();
            await Task.Yield();

            SystemUnderTest.Listen();
            await Task.Yield();

            SystemUnderTest.StopListening();
            await Task.Yield();
        }

        [Then]
        public void MessagesAreReceived()
        {
            Sqs.Received().ReceiveMessageAsync(
                Arg.Any<ReceiveMessageRequest>(),
                Arg.Any<CancellationToken>());
        }

        [Then]
        public void CorrectQueueIsPolled()
        {
            Sqs.Received().ReceiveMessageAsync(
                Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == QueueUrl),
                Arg.Any<CancellationToken>());
        }

        [Then]
        public void TheMaxMessageAllowanceIsGrabbed()
        {
            Sqs.Received().ReceiveMessageAsync(
                Arg.Is<ReceiveMessageRequest>(x => x.MaxNumberOfMessages == 10),
                Arg.Any<CancellationToken>());
        }

        [Then]
        public void MessageIsProcessed()
        {
            SerialisationRegister.Received().DeserializeMessage(
                BodyOfMessageAfterStop);
        }
    }
}