using System;
using JustSaying.Models;
using Newtonsoft.Json;

namespace JustSaying.Messaging.MessageSerialisation
{
    public class NewtonsoftSerialiser : IMessageSerialiser
    {
        private readonly JsonConverter _enumConverter = new Newtonsoft.Json.Converters.StringEnumConverter();

        public Message Deserialise(string message, Type type)
        {
            return (Message)JsonConvert.DeserializeObject(message, type, _enumConverter);
        }

        public string Serialise(Message message)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new[] { _enumConverter }
            };

            return JsonConvert.SerializeObject(message, settings);
        }
    }
}