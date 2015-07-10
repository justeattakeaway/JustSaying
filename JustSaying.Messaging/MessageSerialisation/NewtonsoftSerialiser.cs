using System;
using JustSaying.Models;
using Newtonsoft.Json;

namespace JustSaying.Messaging.MessageSerialisation
{
    public class NewtonsoftSerialiser : IMessageSerialiser
    {
        private readonly JsonSerializerSettings _settings;

        public NewtonsoftSerialiser()
        {
            _settings = null;
        }

        public NewtonsoftSerialiser(JsonSerializerSettings settings) : this()
        {
            this._settings = settings;
        }

        public Message Deserialise(string message, Type type)
        {
            return (Message)JsonConvert.DeserializeObject(message, type, GetJsonSettings());
        }

        public string Serialise(Message message)
        {
            var settings = GetJsonSettings();

            return JsonConvert.SerializeObject(message, settings);
        }

        private JsonSerializerSettings GetJsonSettings()
        {
            if (_settings != null)
                return _settings;
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new JsonConverter[] { new Newtonsoft.Json.Converters.StringEnumConverter() }
            };

        }
    }
}