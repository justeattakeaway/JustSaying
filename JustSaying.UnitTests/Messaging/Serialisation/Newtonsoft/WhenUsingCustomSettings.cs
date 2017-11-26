using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace JustSaying.Messaging.UnitTests.Serialisation.Newtonsoft
{
    public class WhenUsingCustomSettings : XBehaviourTest<NewtonsoftSerialiser>
    {
        private MessageWithEnum _messageOut;
        private string _jsonMessage;

        protected override NewtonsoftSerialiser CreateSystemUnderTest()
        {
            return new NewtonsoftSerialiser(new JsonSerializerSettings());
        }

        protected override void Given()
        {
            _messageOut = new MessageWithEnum(Values.Two);
        }

        public string GetMessageInContext(MessageWithEnum message)
        {
            var context = new { Subject = message.GetType().Name, Message = SystemUnderTest.Serialise(message, false) };
            return JsonConvert.SerializeObject(context);
        }

        protected override void When()
        {
            _jsonMessage = GetMessageInContext(_messageOut);
        }

        [Fact]
        public void MessageHasBeenCreated()
        {
            _messageOut.ShouldNotBeNull();
        }

        [Fact]
        public void EnumsAreNotRepresentedAsStrings()
        {
            _jsonMessage.ShouldContain("EnumVal");
            _jsonMessage.ShouldNotContain("Two");        }
    }
}
