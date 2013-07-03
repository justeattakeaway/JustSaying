using System.Linq;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustEat.AwsTools;
using JustEat.Testing;
using NSubstitute;
using SimplesNotificationStack.Messaging.MessageHandling;
using SimplesNotificationStack.Messaging.MessageSerialisation;
using SimplesNotificationStack.Messaging.Messages.CustomerCommunication;
using SimplesNotificationStack.Messaging.Messages.Sms;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class BaseQueuePollingTest : BehaviourTest<SimplesNotificationStack.AwsTools.SqsNotificationListener>
    {
        protected const string QueueUrl = "url";
        protected readonly AmazonSQS Sqs = Substitute.For<AmazonSQS>();
        protected readonly IMessageSerialiser<CustomerOrderRejectionSms> Serialiser = Substitute.For<IMessageSerialiser<CustomerOrderRejectionSms>>();
        protected CustomerOrderRejectionSms DeserialisedMessage;
        protected const string MessageBody = "object";
        protected readonly IHandler<CustomerOrderRejectionSms> Handler = Substitute.For<IHandler<CustomerOrderRejectionSms>>();
        private readonly string _messageTypeString = typeof(CustomerOrderRejectionSms).ToString();

        protected override SimplesNotificationStack.AwsTools.SqsNotificationListener CreateSystemUnderTest()
        {
            var serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
            serialisationRegister.GetSerialiser(_messageTypeString).Returns(Serialiser);
            return new SimplesNotificationStack.AwsTools.SqsNotificationListener(new SqsQueueByUrl(QueueUrl, Sqs), serialisationRegister);
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
}