using JustEat.Testing;
using NSubstitute;
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
            SystemUnderTest.AddSerialiser(Substitute.For<IMessageSerialiser<CustomerOrderRejectionSms>>());
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
