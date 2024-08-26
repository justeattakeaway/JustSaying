using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers;

internal class FakeBodySerializer(string messageBody) : IMessageBodySerializer
{
    public string Serialize(Message message) => messageBody;
    Message IMessageBodySerializer.Deserialize(string message) => throw new NotImplementedException();
}

internal class FakeBodyDeserializer(Message messageToReturn) : IMessageBodySerializer
{
    string IMessageBodySerializer.Serialize(Message message) =>  throw new NotImplementedException();
    public Message Deserialize(string message) => messageToReturn;
}
