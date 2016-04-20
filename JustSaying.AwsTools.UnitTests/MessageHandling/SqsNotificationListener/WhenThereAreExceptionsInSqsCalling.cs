using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenThereAreExceptionsInSqsCalling : BaseQueuePollingTest
    {
        private int _sqsCallCounter;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        protected override void Given()
        {
            Logging.ToConsole();

            Sqs = Substitute.For<IAmazonSQS>();
            SerialisationRegister = Substitute.For<IMessageSerialisationRegister>();
            Monitor = Substitute.For<IMessageMonitor>();
            Handler = Substitute.For<IHandlerAsync<GenericMessage>>();
            GenerateResponseMessage(MessageTypeString, Guid.NewGuid());

            DeserialisedMessage = new GenericMessage { RaisingComponent = "Component" };
            
            Sqs.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(_ =>  ExceptionOnFirstCall());
        }

        private Task ExceptionOnFirstCall()
        {
            _sqsCallCounter++;
            if (_sqsCallCounter == 1)
            {
                throw new TestException("testing the failure on first call");
            }
            if (_sqsCallCounter == 2)
            {
                Tasks.DelaySendDone(_tcs);
            }

            return Task.FromResult(new ReceiveMessageResponse());
        }

        protected override async Task When()
        {
            SystemUnderTest.AddMessageHandler(() => Handler);
            SystemUnderTest.Listen();

            // wait until it's done
            await Tasks.WaitWithTimeoutAsync(_tcs.Task);
            SystemUnderTest.StopListening();
            await Task.Yield();
        }

        [Then]
        public void QueueIsPolledMoreThanOnce()
        {
            Assert.That(_sqsCallCounter, Is.GreaterThan(1));
        }
    }
}