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

    [TestFixture]
    public class SerialisationMapRegistration_x
    {
        [Test]
        [Ignore("works but statics causing some issues in test runner")]
        public void MapSaysNotRegisteredBeforeStackRegisterCall()
        {
            Assert.False(SerialisationMap.IsRegistered);
        }
    }

    [TestFixture]
    public class SerialisationMapRegistration_y
    {
        [Test]
        public void MapSaysRegisteredAfterStackRegisterCall()
        {
            SimplesNotificationStack.Messaging.Stack.Register();
            Assert.True(SerialisationMap.IsRegistered);
        }
    }
}
