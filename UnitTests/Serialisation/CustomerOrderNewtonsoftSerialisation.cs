using NUnit.Framework;
using Newtonsoft.Json;
using SimplesNotificationStack.Messaging;
using SimplesNotificationStack.Messaging.MessageSerialisation;
using SimplesNotificationStack.Messaging.Messages.CustomerCommunication;
using SimplesNotificationStack.Messaging.Messages.Sms;

namespace UnitTests.Serialisation
{
    [TestFixture]
    public class CustomerOrderNewtonsoftSerialisation
    {
        private CustomerOrderRejectionSms _originalObject;
        private string _serializeObject;

        [TestFixtureSetUp]
        public void SetupFixture()
        {
            _originalObject = new CustomerOrderRejectionSms(1, 2, "3", SmsCommunicationActivity.ConfirmedReceived);
            _serializeObject = JsonConvert.SerializeObject(_originalObject);
        }

        [Test]
        public void Deserialisation()
        {
            // Act
            var result = SerialisationMap.GetMap(typeof(CustomerOrderRejectionSms).ToString()).Deserialised(_serializeObject) as CustomerOrderRejectionSms;

            // Assert
            Assert.AreEqual(1, result.OrderId);
            Assert.AreEqual(2, result.CustomerId);
        }

        [Test]
        public void Serialisation()
        {
            // Act
            var result = SerialisationMap.GetMap(typeof (CustomerOrderRejectionSms).ToString()).Serialised(_originalObject);

            // Assert
            Assert.AreEqual(_serializeObject, result);
        }
    }
}