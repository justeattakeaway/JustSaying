using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Serialization.SystemTextJson
{
    public class WhenSerializingAndDeserializing : XBehaviourTest<SystemTextJsonSerializer>
    {
        private MessageWithEnum _messageOut;
        private MessageWithEnum _messageIn;
        private string _jsonMessage;

        protected override void Given()
        {
            _messageOut = new MessageWithEnum() { EnumVal = Value.Two };
        }

        protected override void WhenAction()
        {
            _jsonMessage = SystemUnderTest.Serialize(_messageOut, false, _messageOut.GetType().Name);
            _messageIn = SystemUnderTest.Deserialize(_jsonMessage, typeof(MessageWithEnum)) as MessageWithEnum;
        }

        [Fact]
        public void MessageHasBeenCreated()
        {
            _messageOut.ShouldNotBeNull();
        }

        [Fact]
        public void MessagesContainSameDetails()
        {
            _messageOut.EnumVal.ShouldBe(_messageIn.EnumVal);
        }

        [Fact]
        public void EnumsAreRepresentedAsStrings()
        {
            _jsonMessage.ShouldContain("EnumVal");
            _jsonMessage.ShouldContain("Two");
        }
    }
}
