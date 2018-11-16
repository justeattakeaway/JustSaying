using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Serialisation.Newtonsoft
{
    public class DealingWithPotentiallyMissingConversation : XBehaviourTest<NewtonsoftSerialiser>
    {
        private MessageWithEnum _messageOut;
        private MessageWithEnum _messageIn;
        private string _jsonMessage;
        protected override void Given()
        {
            _messageOut = new MessageWithEnum(Value.Two);
        }

        protected override void When()
        {
            _jsonMessage = SystemUnderTest.Serialise(_messageOut, false, _messageOut.GetType().Name);

            //add extra property to see what happens:
            _jsonMessage = _jsonMessage.Replace("{__", "{\"New\":\"Property\",__");
            _messageIn = SystemUnderTest.Deserialise(_jsonMessage, typeof(MessageWithEnum)) as MessageWithEnum;
        }

        [Fact]
        public void
            ItDoesNotHaveConversationPropertySerialisedBecauseItIsNotSet_ThisIsForBackwardsCompatibilityWhenWeDeploy()
        {
            _jsonMessage.ShouldNotContain("Conversation");
        }

        [Fact]
        public void DeserialisedMessageHasEmptyConversation_ThisIsForBackwardsCompatibilityWhenWeDeploy()
        {
            _messageIn.Conversation.ShouldBeNull();
        }
    }
}
