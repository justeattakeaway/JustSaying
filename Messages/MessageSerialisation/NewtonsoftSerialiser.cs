using Newtonsoft.Json;
using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Messaging.MessageSerialisation
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