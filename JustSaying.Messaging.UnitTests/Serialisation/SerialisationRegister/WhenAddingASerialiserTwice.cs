using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.Serialisation.SerialisationRegister
{
    public class WhenAddingASerialiserTwice : BehaviourTest<MessageSerialisationRegister>
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            SystemUnderTest.AddSerialiser<Message>(Substitute.For<IMessageSerialiser>());
            SystemUnderTest.AddSerialiser<Message>(Substitute.For<IMessageSerialiser>());
        }

        [Then]
        public void ExceptionIsNotThrown()
        {
            Assert.IsNull(ThrownException);
        }

        [Then]
        public void ExceptionThrownIfFormatNotSupported()
        {
            Assert.Throws<MessageFormatNotSupportedException>(() => SystemUnderTest.DeserializeMessage(string.Empty));
        }
    }
}