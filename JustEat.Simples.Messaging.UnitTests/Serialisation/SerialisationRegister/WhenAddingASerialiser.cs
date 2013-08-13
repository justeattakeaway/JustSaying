using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages.CustomerCommunication;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests.Serialisation.SerialisationRegister
{
    public class WhenAddingASerialiser : BehaviourTest<MessageSerialisationRegister>
    {
        protected override void Given()
        {
            //
        }

        protected override void When()
        {
            SystemUnderTest.AddSerialiser<CustomerOrderRejectionSms>(Substitute.For<IMessageSerialiser<CustomerOrderRejectionSms>>());
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