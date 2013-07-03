using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustEat.AwsTools;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;
using SimplesNotificationStack.AwsTools;
using SimplesNotificationStack.Messaging.MessageHandling;
using SimplesNotificationStack.Messaging.MessageSerialisation;
using SimplesNotificationStack.Messaging.Messages.CustomerCommunication;
using SimplesNotificationStack.Messaging.Messages.Sms;

namespace AwsTools.UnitTests
{
    public class BaseQueuePollingTest : BehaviourTest<SqsNotificationListener>
    {
        protected const string QueueUrl = "url";
        protected readonly AmazonSQS Sqs = Substitute.For<AmazonSQS>();
        protected readonly IMessageSerialiser<CustomerOrderRejectionSms> Serialiser = Substitute.For<IMessageSerialiser<CustomerOrderRejectionSms>>();
        protected CustomerOrderRejectionSms DeserialisedMessage;
        protected const string MessageBody = "object";
        protected readonly IHandler<CustomerOrderRejectionSms> Handler = Substitute.For<IHandler<CustomerOrderRejectionSms>>();
        private readonly string _messageTypeString = typeof(CustomerOrderRejectionSms).ToString();

        protected override SqsNotificationListener CreateSystemUnderTest()
        {
            var serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
            serialisationRegister.GetSerialiser(_messageTypeString).Returns(Serialiser);
            return new SqsNotificationListener(new SqsQueueByUrl(QueueUrl, Sqs), serialisationRegister);
        }

        protected override void Given()
        {
            var response = new ReceiveMessageResponse
            {
                ReceiveMessageResult = new ReceiveMessageResult
                                {
                                Message = new[] {       
                                    new Message
                                        {
                                            Body = "{\"Subject\":\"" + _messageTypeString + "\"," + 
                                                "\"Message\":\"" + MessageBody + "\"}"
                                        },
                                    new Message
                                        {
                                            Body = "{\"Subject\":\"SOME_UNKNOWN_MESSAGE\"," + 
                                                "\"Message\":\"SOME_RANDOM_MESSAGE\"}"
                                        }}.ToList(),
                                }};
            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(response);

            DeserialisedMessage = new CustomerOrderRejectionSms(1, 2, "3", SmsCommunicationActivity.ConfirmedReceived);
            Serialiser.Deserialise(Arg.Any<string>()).Returns(DeserialisedMessage);

            Handler.HandlesMessageType.Returns(typeof(CustomerOrderRejectionSms));
        }

        protected override void When()
        {
            SystemUnderTest.AddMessageHandler(Handler);
            SystemUnderTest.Listen();
        }
    }

    public class WhenListeningStarts : BaseQueuePollingTest
    {
        [Then]
        public void CorrectQueueIsPolled()
        {
            Sqs.Received().ReceiveMessage(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == QueueUrl));
        }

        [Then]
        public void TheMaxMessageAllowanceIsGrabbed()
        {
            Sqs.Received().ReceiveMessage(Arg.Is<ReceiveMessageRequest>(x => x.MaxNumberOfMessages == 10));
        }
    }

    public class WhenPassingAHandledAndUnhandledMessage : BaseQueuePollingTest
    {
        [Then]
        public void MessagesGetDeserialisedByCorrectHandler()
        {
            Serialiser.Received().Deserialise(MessageBody);
        }

        [Then]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Handler.Received().Handle(DeserialisedMessage);
        }

        [Then]
        public void AllMessagesAreClearedFromQueue()
        {
            Serialiser.Received(1).Deserialise(Arg.Any<string>());
            Sqs.Received(2).DeleteMessage(Arg.Any<DeleteMessageRequest>());
        }
    }

    /*
    Some more:
     * 1. Multiple handling of same message with different handlers
     * 2. Message failed processing does not get deleted
     * 3. etc
    */
}
