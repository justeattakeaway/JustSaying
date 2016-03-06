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
            Sqs = Substitute.For<IAmazonSQS>();
            SerialisationRegister = Substitute.For<IMessageSerialisationRegister>();
            Monitor = Substitute.For<IMessageMonitor>();
            Handler = Substitute.For<IHandler<GenericMessage>>();
            GenerateResponseMessage(MessageTypeString, Guid.NewGuid());

            DeserialisedMessage = new GenericMessage { RaisingComponent = "Component" };
            
            Sqs.When(x => x.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>()))
                .Do(_ =>
                {
                    _sqsCallCounter++;
                    Patiently.DelaySendDone(_tcs);
                    throw new Exception();
                });
        }

        protected override async Task When()
        {
 
            SystemUnderTest.AddMessageHandler(() => Handler);
            SystemUnderTest.Listen();

            // wait until it's done
            await Patiently.WaitWithTimeoutAsync(_tcs.Task);
        }

        [Then]
        public void QueueIsPolledMoreThanOnce()
        {
            Assert.That(_sqsCallCounter, Is.GreaterThan(1));
        }

        public override void PostAssertTeardown()
        {
            SystemUnderTest.StopListening();
            base.PostAssertTeardown();
        }
    }
}