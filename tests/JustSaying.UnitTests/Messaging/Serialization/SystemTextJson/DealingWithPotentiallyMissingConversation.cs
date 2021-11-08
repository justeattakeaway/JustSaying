using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Serialization.SystemTextJson
{
    public class DealingWithPotentiallyMissingConversation : XBehaviourTest<SystemTextJsonSerializer>
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

            // Add extra property to see what happens:
            _jsonMessage = _jsonMessage.Replace("{__", "{\"New\":\"Property\",__", StringComparison.OrdinalIgnoreCase);
            _messageIn = SystemUnderTest.Deserialize(_jsonMessage, typeof(MessageWithEnum)) as MessageWithEnum;
        }

        [Fact]
        public void ItDoesNotHaveConversationPropertySerializedBecauseItIsNotSet_ThisIsForBackwardsCompatibilityWhenWeDeploy()
        {
            _jsonMessage.ShouldNotContain("Conversation");
        }

        [Fact]
        public void DeserializedMessageHasEmptyConversation_ThisIsForBackwardsCompatibilityWhenWeDeploy()
        {
            _messageIn.Conversation.ShouldBeNull();
        }
    }
}
