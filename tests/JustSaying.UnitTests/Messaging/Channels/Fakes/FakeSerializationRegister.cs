using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers
{
    public class FakeSerializationRegister : IMessageSerializationRegister
    {
        public Func<SimpleMessage> DefaultDeserializedMessage { get; set; }

        public IList<string> ReceivedDeserializationRequests { get; }

        public FakeSerializationRegister()
        {
            var defaultMessage = new SimpleMessage
            {
                RaisingComponent = "Component",
                Id = Guid.NewGuid()
            };
            DefaultDeserializedMessage = () => defaultMessage;
            ReceivedDeserializationRequests = new List<string>();
        }

        public MessageWithAttributes DeserializeMessage(string body)
        {
            ReceivedDeserializationRequests.Add(body);
            return new MessageWithAttributes(DefaultDeserializedMessage(), new MessageAttributes());
        }

        public string Serialize(Message message, bool serializeForSnsPublishing)
        {
            return "";
        }

        public void AddSerializer<T>() where T : Message
        { }
    }
}
