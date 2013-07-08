using NUnit.Framework;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages.CustomerCommunication;

namespace UnitTests.Serialisation.SerialisationRegister
{
    [TestFixture]
    public class WhenCreated
    {
        [Test]
        public void ReflectionCreatesMappings()
        {
            var target = new ReflectedMessageSerialisationRegister();
            
            Assert.NotNull(target.GetSerialiser(typeof(CustomerOrderRejectionSms).Name));
        }
    }
}
