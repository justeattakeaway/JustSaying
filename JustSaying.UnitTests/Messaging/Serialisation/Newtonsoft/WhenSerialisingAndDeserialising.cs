using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Serialisation.Newtonsoft
{
    public class WhenSerialisingAndDeserialising : XBehaviourTest<NewtonsoftSerialiser>
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
            _jsonMessage = SystemUnderTest.Serialise(_messageOut, false, _messageOut.GetType().Name);
            _messageIn = SystemUnderTest.Deserialise(_jsonMessage, typeof(MessageWithEnum)) as MessageWithEnum;
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
            _messageOut.RaisingComponent.ShouldBe(_messageIn.RaisingComponent);
            _messageOut.TimeStamp.ShouldBe(_messageIn.TimeStamp);
        }
        
        [Fact]
        public void EnumsAreRepresentedAsStrings()
        {
            _jsonMessage.ShouldContain("EnumVal");
            _jsonMessage.ShouldContain("Two");
        }
    }
}
