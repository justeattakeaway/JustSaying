using System;
using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.Messaging.UnitTests.Serialisation.SerialisationRegister
{
    public class WhenDeserializingMessage : XBehaviourTest<MessageSerialisationRegister>
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

        [Fact]
        public void ThrowsMessageFormatNotSupportedWhenMessabeBodyIsUnserializable()
        {
            new Action(() => SystemUnderTest.DeserializeMessage(string.Empty)).ShouldThrow<MessageFormatNotSupportedException>();
        }

        [Fact]
        public void TheMappingContainsTheSerialiser()
        {
            SystemUnderTest.DeserializeMessage(messageBody).ShouldNotBeNull();
        }

    }
}
