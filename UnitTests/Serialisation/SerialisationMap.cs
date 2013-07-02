using NUnit.Framework;
using SimplesNotificationStack.Messaging.MessageSerialisation;
using SimplesNotificationStack.Messaging.Messages.CustomerCommunication;

namespace UnitTests.Serialisation
{
    [TestFixture]
    public class SerialisationMapRegistration
    {
        [Test]
        public void MapsAreReturnedOnceRegistered()
        {
            // Arrange
            SimplesNotificationStack.Messaging.Stack.Register();

            // Act
            var result = SerialisationMap.GetMap(typeof(CustomerOrderRejectionSms).ToString());

            // Assert
            Assert.NotNull(result);
        }
    }
}
