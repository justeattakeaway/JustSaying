using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Serialization.SerializationRegister
{
    public class WhenAddingASerializerTwice : XBehaviourTest<MessageSerializationRegister>
    {
        protected override MessageSerializationRegister CreateSystemUnderTest() =>
            new MessageSerializationRegister(new NonGenericMessageSubjectProvider());

        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void WhenAction()
        {
            SystemUnderTest.AddSerializer<Message>(Substitute.For<IMessageSerializer>());
            SystemUnderTest.AddSerializer<Message>(Substitute.For<IMessageSerializer>());
        }

        [Fact]
        public void ExceptionIsNotThrown()
        {
            ThrownException.ShouldBeNull();
        }
    }
}
