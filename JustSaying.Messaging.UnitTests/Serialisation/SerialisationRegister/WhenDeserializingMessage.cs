using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.Serialisation.SerialisationRegister
{
    public class WhenDeserializingMessage : BehaviourTest<MessageSerialisationRegister>
    {
        private class CustomMessage : Message
        {
        }

        private string messageBody = "msgBody";
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            var messageSerialiser = Substitute.For<IMessageSerialiser>();
            messageSerialiser.GetMessageType(messageBody).Returns(typeof(CustomMessage).Name);
            messageSerialiser.Deserialise(messageBody, typeof (CustomMessage)).Returns(new CustomMessage());
            SystemUnderTest.AddSerialiser<CustomMessage>(messageSerialiser);
        }

        [Then]
        public void ThrowsMessageFormatNotSupportedWhenMessabeBodyIsUnserializable()
        {
            Assert.Throws<MessageFormatNotSupportedException>(() => SystemUnderTest.DeserializeMessage(string.Empty));
        }

        [Then]
        public void TheMappingContainsTheSerialiser()
        {
            Assert.NotNull(SystemUnderTest.DeserializeMessage(messageBody));
        }

    }
}