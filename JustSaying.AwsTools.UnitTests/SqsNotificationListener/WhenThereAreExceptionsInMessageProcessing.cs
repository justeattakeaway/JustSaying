using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS.Model;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustBehave;
using NSubstitute;
using JustSaying.TestingFramework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    using SqsNotificationListener = JustSaying.AwsTools.SqsNotificationListener;

    public class WhenThereAreExceptionsInMessageProcessing : 
        BehaviourTest<SqsNotificationListener>
    {
        private readonly ISqsClient _sqs = Substitute.For<ISqsClient>();
        private readonly IMessageSerialisationRegister _serialisationRegister = 
            Substitute.For<IMessageSerialisationRegister>();
        
        private int _callCount;

        protected override SqsNotificationListener CreateSystemUnderTest()
        {
            return new SqsNotificationListener(
                new SqsQueueByUrl("", _sqs), 
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

            _sqs.Region.Returns(RegionEndpoint.EUWest1);
        }

        protected override void When()
        {
            SystemUnderTest.Listen();
        }

        [Then]
        public async Task TheListenerDoesNotDie()
        {
            await Patiently.AssertThatAsync(() => _callCount >= 3);
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