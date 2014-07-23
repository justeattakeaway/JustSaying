//using JustSaying.Models;
//using ServiceStack.Text;

//namespace JustSaying.Messaging.MessageSerialisation
//{
//    public class ServiceStackSerialiser<T> : IMessageSerialiser<Message> where T : Message
//    {
//        public Message Deserialise(string message)
//        {
//            return JsonSerializer.DeserializeFromString<T>(message);
//        }

//        public string Serialise(Message message)
//        {
//            return JsonSerializer.SerializeToString(message);
//        }
//    }
//}