using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.TestingFramework;
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
                            GenerateResponseMessage(
                                SubjectOfMessageAfterStop, 
                                Guid.NewGuid())), 
                    _ => Task.FromResult(
                            new ReceiveMessageResponse
                            {
                                Messages = new List<Message>()
                            }));
        }

        protected override void When()
        {
            base.When();

            SystemUnderTest.StopListening();
            
            SystemUnderTest.Listen();
        }

        [Then]
        public async Task CorrectQueueIsPolled()
        {
            await Patiently.VerifyExpectationAsync(() =>
                Sqs.Received().ReceiveMessageAsync(
                    Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == QueueUrl),
                    Arg.Any<CancellationToken>()));
        }

        [Then]
        public async Task TheMaxMessageAllowanceIsGrabbed()
        {
            await Patiently.VerifyExpectationAsync(() => 
                Sqs.Received().ReceiveMessageAsync(
                    Arg.Is<ReceiveMessageRequest>(x => x.MaxNumberOfMessages == 10),
                    Arg.Any<CancellationToken>()));
        }

        [Then]
        public async Task MessageIsProcessed()
        {
            await Patiently.VerifyExpectationAsync(() => 
                SerialisationRegister.Received().DeserializeMessage(
                    BodyOfMessageAfterStop));
        }

        public override void PostAssertTeardown()
        {
            base.PostAssertTeardown();
            SystemUnderTest.StopListening();
        }
    }
}