using NUnit.Framework;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages.CustomerCommunication;

namespace UnitTests.Serialisation.ReflectedSerialisationRegister
{
    [TestFixture]
    public class WhenCreated
    {
        [Test]
        public void MappingsCanBeRetreivedByStringType()
        {
            var target = new ReflectedMessageSerialisationRegister();
            
            Assert.NotNull(target.GetSerialiser(typeof(CustomerOrderRejectionSms).Name));
        }

        [Test]
        public void MappingsCanBeRetreivedStronglyTyped()
        {
            var target = new ReflectedMessageSerialisationRegister();

            Assert.NotNull(target.GetSerialiser(typeof(CustomerOrderRejectionSms)));
        }
    }
}
