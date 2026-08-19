using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers;

internal class FakeBodySerializer(string messageBody) : IMessageBodySerializer
{
    public string Serialize(object message) => messageBody;
    object IMessageBodySerializer.Deserialize(string message) => throw new NotImplementedException();
}

internal class FakeBodyDeserializer(Message messageToReturn) : IMessageBodySerializer
{
    string IMessageBodySerializer.Serialize(object message) =>  throw new NotImplementedException();
    public object Deserialize(string message) => messageToReturn;
}
