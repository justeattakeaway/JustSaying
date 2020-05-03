using System;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Serialization.SerializationRegister
{
    public class WhenDeserializingMessage : XBehaviourTest<MessageSerializationRegister>
    {
        private class CustomMessage : Message
        {
        }

        protected override MessageSerializationRegister CreateSystemUnderTest() =>
            new MessageSerializationRegister(new NonGenericMessageSubjectProvider());

        private string messageBody = "msgBody";
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void WhenAction()
        {
            var messageSerializer = Substitute.For<IMessageSerializer>();
            messageSerializer.GetMessageSubject(messageBody).Returns(typeof(CustomMessage).Name);
            messageSerializer.Deserialize(messageBody, typeof(CustomMessage)).Returns(new CustomMessage());
            SystemUnderTest.AddSerializer<CustomMessage>(messageSerializer);
        }

        [Fact]
        public void ThrowsMessageFormatNotSupportedWhenMessabeBodyIsUnserializable()
        {
            new Action(() => SystemUnderTest.DeserializeMessage(string.Empty)).ShouldThrow<MessageFormatNotSupportedException>();
        }

        [Fact]
        public void TheMappingContainsTheSerializer()
        {
            SystemUnderTest.DeserializeMessage(messageBody).ShouldNotBeNull();
        }

    }
}
