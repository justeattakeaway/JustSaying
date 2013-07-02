using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Messaging.MessageSerialisation
{
    public interface ISerialiser<out T> where T : Message
    {
        string Key { get; }
        T Deserialised(string message);
        string Serialised(Message message);
    }

    internal class NewtonsoftBaseSerialiser<T> : ISerialiser<Message> where T : Message
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

    public static class SerialisationMap
    {
        private static readonly List<ISerialiser<Message>> Map = new List<ISerialiser<Message>>();

        internal static void Register(ISerialiser<Message> serialisation)
        {
            Map.Add(serialisation);
        }

        public static ISerialiser<Message> GetMap(string objectType)
        {
            return Map.FirstOrDefault(x => x.Key == objectType);
        }
    }
}
