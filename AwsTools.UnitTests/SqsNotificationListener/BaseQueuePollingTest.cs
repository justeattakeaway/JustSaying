using System;
using System.Linq;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Testing;
using NSubstitute;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages.CustomerCommunication;
using JustEat.Simples.NotificationStack.Messaging.Messages.Sms;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class BaseQueuePollingTest : BehaviourTest<JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener>
    {
        protected const string QueueUrl = "url";
        protected readonly AmazonSQS Sqs = Substitute.For<AmazonSQS>();
        protected readonly IMessageSerialiser<CustomerOrderRejectionSms> Serialiser = Substitute.For<IMessageSerialiser<CustomerOrderRejectionSms>>();
        protected CustomerOrderRejectionSms DeserialisedMessage;
        protected const string MessageBody = "object";
        protected readonly IHandler<CustomerOrderRejectionSms> Handler = Substitute.For<IHandler<CustomerOrderRejectionSms>>();
        private readonly string _messageTypeString = typeof(CustomerOrderRejectionSms).ToString();

        protected override JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener CreateSystemUnderTest()
        {
            var serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
            serialisationRegister.GetSerialiser(_messageTypeString).Returns(Serialiser);
            return new JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener(new SqsQueueByUrl(QueueUrl, Sqs), serialisationRegister);
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
            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => response, x => new ReceiveMessageResponse());

            DeserialisedMessage = new CustomerOrderRejectionSms(1, 2, "3", SmsCommunicationActivity.ConfirmedReceived);
            Serialiser.Deserialise(Arg.Any<string>()).Returns(x => DeserialisedMessage);
        }

        protected override void When()
        {
            SystemUnderTest.AddMessageHandler(Handler);
            SystemUnderTest.Listen();
        }
    }
}