using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    using SqsNotificationListener = AwsTools.MessageHandling.SqsNotificationListener;

    public class WhenThereAreExceptionsInMessageProcessing : 
        BehaviourTest<SqsNotificationListener>
    {
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private readonly IMessageSerialisationRegister _serialisationRegister = 
            Substitute.For<IMessageSerialisationRegister>();
        
        private int _callCount;

        protected override SqsNotificationListener CreateSystemUnderTest()
        {
            return new SqsNotificationListener(
                new SqsQueueByUrl(RegionEndpoint.EUWest1, "", _sqs), 
                _serialisationRegister, 
                Substitute.For<IMessageMonitor>());
        }

        protected override void Given()
        {
            _serialisationRegister
                .DeserializeMessage(Arg.Any<string>())
                .Returns(x => { throw new Exception(); });
            _sqs.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult(GenerateEmptyMessage()));

            _sqs.When(x => x.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>()))
                .Do(x => _callCount++);
        }

        protected override void When()
        {
            SystemUnderTest.Listen();
        }

        [Then]
        public void TheListenerDoesNotDie()
        {
            Assert.That(_callCount, Is.GreaterThanOrEqualTo(3));
        }

        public override void PostAssertTeardown()
        {
            base.PostAssertTeardown();
            SystemUnderTest.StopListening();
        }

        private ReceiveMessageResponse GenerateEmptyMessage()
        {
            return new ReceiveMessageResponse
            {
                Messages = new List<Message>()
            };
        }
    }
}