using JustBehave;
using JustSaying.Messaging.Extensions;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.Serialisation.Newtonsoft
{
    public class WhenUsingCustomSettings: BehaviourTest<NewtonsoftSerialiser>
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
            var context = new { Subject = message.GetType().ToKey(), Message = SystemUnderTest.Serialise(message) };
            return JsonConvert.SerializeObject(context);
        }

        protected override void When()
        {
            _jsonMessage = GetMessageInContext(_messageOut);
        }

        [Then]
        public void MessageHasBeenCreated()
        {
            Assert.NotNull(_messageOut);
        }

        [Then]
        public void EnumsAreNotRepresentedAsStrings()
        {
            Assert.That(_jsonMessage, Is.StringContaining("EnumVal"));
            Assert.That(_jsonMessage, Is.Not.StringContaining("Two"));
        }
    }
}