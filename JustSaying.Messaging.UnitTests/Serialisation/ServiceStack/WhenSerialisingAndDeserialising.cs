using JustEat.Testing;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.Serialisation.ServiceStack
{
    public class WhenSerialisingAndDeserialising : BehaviourTest<ServiceStackSerialiser<MessageWithEnum>>
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
            _messageIn = SystemUnderTest.Deserialise(_jsonMessage) as MessageWithEnum;
        }

        [Then]
        public void MessageHasBeenCreated()
        {
            Assert.NotNull(_messageOut);
        }

        [Then]
        public void MessagesContainSameDetails()
        {
            Assert.AreEqual(_messageIn.EnumVal, _messageOut.EnumVal);
            Assert.AreEqual(_messageIn.RaisingComponent, _messageOut.RaisingComponent);
            //Assert.AreEqual(_messageIn.TimeStamp, _messageOut.TimeStamp);
            // ToDo: Sort timestamp issue!
        }
        
        [Then]
        public void EnumsAreRepresentedAsStrings()
        {
            Assert.That(_jsonMessage.Contains("EnumVal"));
            Assert.That(_jsonMessage.Contains("Two"));
        }
    }
}
