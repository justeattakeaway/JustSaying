using JustEat.Simples.NotificationStack.Messaging.Messages;
using ServiceStack.Text;

namespace JustEat.Simples.NotificationStack.Messaging.MessageSerialisation
{
    public class ServiceStackSerialiser<T> : IMessageSerialiser<Message> where T : Message
    {
        public Message Deserialise(string message)
        {
            return JsonSerializer.DeserializeFromString<T>(message);
        }

        public string Serialise(Message message)
        {
            return JsonSerializer.SerializeToString(message);
        }
    }
}