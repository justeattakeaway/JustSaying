using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

public interface IMessageBodySerializer
{
    string Serialize(Message message);
    Message Deserialize(string message);
}
