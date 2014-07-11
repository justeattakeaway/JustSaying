using System;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustBehave;
using NSubstitute;
using JustSaying.TestingFramework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenThereAreExceptionsInMessageProcessing : BehaviourTest<JustSaying.AwsTools.SqsNotificationListener>
    {
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        
        private int _callCount;

        protected override JustSaying.AwsTools.SqsNotificationListener CreateSystemUnderTest()
        {
            return new JustSaying.AwsTools.SqsNotificationListener(new SqsQueueByUrl("", _sqs), _serialisationRegister, Substitute.For<IMessageMonitor>());
        }

        protected override void Given()
        {
            _serialisationRegister.GetSerialiser(Arg.Any<string>()).Returns(x => { throw new Exception(); });
            _sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => GenerateEmptyMessage());
            _sqs.When(x => x.ReceiveMessage(Arg.Any<ReceiveMessageRequest>())).Do(x => _callCount++);
        }

        protected override void When()
        {
            SystemUnderTest.Listen();
        }

        [Then]
        public void TheListenerDoesNotDie()
        {
            Patiently.AssertThat(() => _callCount >= 3);
        }

        public override void PostAssertTeardown()
        {
            base.PostAssertTeardown();
            SystemUnderTest.StopListening();
        }

        private ReceiveMessageResponse GenerateEmptyMessage()
        {
            return new ReceiveMessageResponse { Messages = new System.Collections.Generic.List<Message>() };
        }
    }
}