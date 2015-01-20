using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.Serialisation.Newtonsoft
{
    public class DealingWithPotentiallyMissingConversation : BehaviourTest<NewtonsoftSerialiser>
    {
        private MessageWithEnum _messageOut;
        private MessageWithEnum _messageIn;
        private string _jsonMessage;
        protected override void Given()
        {
            _messageOut = new MessageWithEnum(Values.Two);
        }

        protected override void When()
        {
            _jsonMessage = SystemUnderTest.Serialise(_messageOut);

            //add extra property to see what happens:
            _jsonMessage = _jsonMessage.Replace("{__", "{\"New\":\"Property\",__");
            _messageIn = SystemUnderTest.Deserialise(_jsonMessage, typeof(MessageWithEnum)) as MessageWithEnum;
        }

        [Then]
        public void
            ItDoesNotHaveConversationPropertySerialisedBecauseItIsNotSet_ThisIsForBackwardsCompatibilityWhenWeDeploy()
        {
            Assert.That(_jsonMessage, Is.Not.StringContaining("Conversation"));
        }

        [Then]
        public void DeserialisedMessageHasEmptyConversation_ThisIsForBackwardsCompatibilityWhenWeDeploy()
        {
            Assert.IsNull(_messageIn.Conversation);
        }
    }
}
