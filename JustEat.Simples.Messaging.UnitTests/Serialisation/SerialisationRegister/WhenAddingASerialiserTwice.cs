using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages.CustomerCommunication;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests.Serialisation.SerialisationRegister
{
    public class WhenAddingASerialiserTwice : BehaviourTest<MessageSerialisationRegister>
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.AddSerialiser(Substitute.For<IMessageSerialiser<CustomerOrderRejectionSms>>());
            SystemUnderTest.AddSerialiser(Substitute.For<IMessageSerialiser<CustomerOrderRejectionSms>>());
        }

        [Then]
        public void ExceptionIsNotThrown()
        {
            Assert.IsNull(ThrownException);
        }

        [Then]
        public void TheMappingContainsTheSerialiser()
        {
            Assert.NotNull(SystemUnderTest.GetSerialiser(typeof(CustomerOrderRejectionSms).Name));
        }
    }
}