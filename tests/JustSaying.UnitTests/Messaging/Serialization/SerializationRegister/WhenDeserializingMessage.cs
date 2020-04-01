using System;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
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
            new MessageSerializationRegister(
                new NonGenericMessageSubjectProvider(),
                new NewtonsoftSerializationFactory());

        private string messageBody = "{'Subject':'nonexistent'}";
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void WhenAction()
        {
            var messageSerializer = Substitute.For<IMessageSerializer>();
            messageSerializer.GetMessageSubject(messageBody).Returns(typeof(CustomMessage).Name);
            messageSerializer.Deserialize(messageBody, typeof(CustomMessage)).Returns(new CustomMessage());
            SystemUnderTest.AddSerializer<CustomMessage>();
        }

        [Fact]
        public void ThrowsMessageFormatNotSupportedWhenMessabeBodyIsUnserializable()
        {
            Assert.Throws<MessageFormatNotSupportedException>(() => SystemUnderTest.DeserializeMessage(messageBody));
        }
    }
}
