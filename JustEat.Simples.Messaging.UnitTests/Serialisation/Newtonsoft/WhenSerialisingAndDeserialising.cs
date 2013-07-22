using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Testing;
using NUnit.Framework;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages.CustomerCommunication;
using JustEat.Simples.NotificationStack.Messaging.Messages.Sms;

namespace UnitTests.Serialisation.Newtonsoft
{
    public class WhenSerialisingAndDeserialising : BehaviourTest<NewtonsoftSerialiser<CustomerOrderRejectionSms>>
    {
        private CustomerOrderRejectionSms _messageOut;
        private CustomerOrderRejectionSms _messageIn;
        private string _jsonMessage;
        protected override void Given()
        {
            _messageOut = new CustomerOrderRejectionSms(1, 2, "3", SmsCommunicationActivity.Sent){RaisingComponent = Component.OrderEngine};
        }

        protected override void When()
        {
            _jsonMessage = SystemUnderTest.Serialise(_messageOut);
            _messageIn = SystemUnderTest.Deserialise(_jsonMessage) as CustomerOrderRejectionSms;
        }

        [Then]
        public void MessageHasBeenCreated()
        {
            Assert.NotNull(_messageOut);
        }

        [Then]
        public void MessagesContainSameDetails()
        {
            Assert.AreEqual(_messageIn.CommunicationActivity, _messageOut.CommunicationActivity);
            Assert.AreEqual(_messageIn.CustomerId, _messageOut.CustomerId);
            Assert.AreEqual(_messageIn.OrderId, _messageOut.OrderId);
            Assert.AreEqual(_messageIn.TelephoneNumber, _messageOut.TelephoneNumber);
            Assert.AreEqual(_messageIn.RaisingComponent, _messageOut.RaisingComponent);
            //Assert.AreEqual(_messageIn.TimeStamp, _messageOut.TimeStamp);
            // ToDo: Sort timestamp issue!
        }
        
        [Then]
        public void EnumsAreRepresentedAsStrings()
        {
            Assert.That(_jsonMessage.Contains("OrderEngine"));
            Assert.That(_jsonMessage.Contains("Sent"));
        }
    }
}
