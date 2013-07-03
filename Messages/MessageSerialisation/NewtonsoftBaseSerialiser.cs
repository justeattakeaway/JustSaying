using Newtonsoft.Json;
using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Messaging.MessageSerialisation
{
    internal class NewtonsoftBaseSerialiser<T> : IMessageSerialiser<Message> where T : Message
    {
        public string Key { get { return typeof(T).ToString(); } }
        public Message Deserialised(string message)
        {
            return JsonConvert.DeserializeObject<T>(message);
        }

        public string Serialised(Message message)
        {
            return JsonConvert.SerializeObject(message);
        }
    }
}