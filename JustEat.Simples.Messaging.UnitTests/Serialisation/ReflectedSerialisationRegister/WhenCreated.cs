using JustEat.Simples.NotificationStack.Messaging.Messages;
using NUnit.Framework;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;

namespace UnitTests.Serialisation.ReflectedSerialisationRegister
{
    [TestFixture]
    public class WhenCreated
    {
        [Test]
        public void MappingsCanBeRetreivedByStringType()
        {
            var target = new ReflectedMessageSerialisationRegister();
            
            Assert.NotNull(target.GetSerialiser(typeof(Message).Name));
        }

        [Test]
        public void MappingsCanBeRetreivedStronglyTyped()
        {
            var target = new ReflectedMessageSerialisationRegister();

            Assert.NotNull(target.GetSerialiser(typeof(Message)));
        }
    }
}
