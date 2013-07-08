using Newtonsoft.Json;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Messaging.MessageSerialisation
{
    public class NewtonsoftSerialiser<T> : IMessageSerialiser<Message> where T : Message
    {
        public string Key { get { return typeof(T).ToString(); } }
        public Message Deserialise(string message)
        {
            return JsonConvert.DeserializeObject<T>(message);
        }

        public string Serialise(Message message)
        {
            return JsonConvert.SerializeObject(message);
        }
    }
}