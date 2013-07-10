using System;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenThereAreExceptionsInMessageProcessing : BehaviourTest<JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener>
    {
        private readonly AmazonSQS _sqs = Substitute.For<AmazonSQS>();
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private int _callCount;

        protected override JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener CreateSystemUnderTest()
        {
            return new JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener(new SqsQueueByUrl("", _sqs), _serialisationRegister);
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
            Thread.Sleep(80);
        }

        [Then]
        public void TheListenerDoesNotDie()
        {
            Assert.GreaterOrEqual(_callCount, 3);
        }

        private ReceiveMessageResponse GenerateEmptyMessage()
        {
            return new ReceiveMessageResponse { ReceiveMessageResult = new ReceiveMessageResult { Message = new[]{new Message()}.ToList()} };
        }
    }
}