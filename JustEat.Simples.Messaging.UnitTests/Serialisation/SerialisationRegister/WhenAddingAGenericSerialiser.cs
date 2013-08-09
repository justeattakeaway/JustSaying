using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages.CustomerCommunication;
using JustEat.Testing;
using NUnit.Framework;

namespace UnitTests.Serialisation.SerialisationRegister
{
    public class WhenAddingAGenericSerialiser : BehaviourTest<MessageSerialisationRegister>
    {
        private readonly ServiceStackSerialiser<CustomerOrderRejectionSms> _serialiser = new ServiceStackSerialiser<CustomerOrderRejectionSms>();

        protected override void Given() { }

        protected override void When()
        {
            SystemUnderTest.AddSerialiser<CustomerOrderRejectionSms>(_serialiser);
        }

        [Then]
        public void MappingsCanBeRetreivedByStringType()
        {
            Assert.NotNull(SystemUnderTest.GetSerialiser(typeof(CustomerOrderRejectionSms).Name));
        }

        [Test]
        public void MappingsCanBeRetreivedStronglyTyped()
        {
            Assert.NotNull(SystemUnderTest.GetSerialiser(typeof(CustomerOrderRejectionSms)));
        }
    }
}