using Newtonsoft.Json;
using JustSaying.Messaging.Messages;

namespace JustSaying.Messaging.MessageSerialisation
{
    public class NewtonsoftSerialiser<T> : IMessageSerialiser<Message> where T : Message
    {
        private readonly JsonConverter _enumConverter = new Newtonsoft.Json.Converters.StringEnumConverter();

        public Message Deserialise(string message)
        {
            return JsonConvert.DeserializeObject<T>(message, _enumConverter);
        }

        public string Serialise(Message message)
        {
            return JsonConvert.SerializeObject(message, _enumConverter);
        }
    }
}