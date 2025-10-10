using System;
using JustSaying.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            _settings = settings;
        }

        public Message Deserialise(string message, Type type)
        {
            var jsqsMessage = JObject.Parse(message);
            var messageBody = jsqsMessage["Message"].ToString().ApplyDecompressionIfRequired();
            return (Message)JsonConvert.DeserializeObject(messageBody, type, GetJsonSettings());
        }

        public string Serialise(Message message, bool serializeForSnsPublishing, string subject)
        {
            var settings = GetJsonSettings();

            var msg = JsonConvert.SerializeObject(message, settings);

            // AWS SNS service will add Subject and Message properties automatically,
            // so just return plain message
            if (serializeForSnsPublishing)
            {
                return msg;
            }

            // for direct publishing to SQS, add Subject and Message properties manually
            var context = new { Subject = subject, Message = msg };
            return JsonConvert.SerializeObject(context);
        }

        private JsonSerializerSettings GetJsonSettings()
        {
            return _settings ?? new JsonSerializerSettings
                   {
                       NullValueHandling = NullValueHandling.Ignore,
                       Converters = new JsonConverter[] {new Newtonsoft.Json.Converters.StringEnumConverter()}
                   };
        }

        public string GetMessageSubject(string sqsMessge)
        {
            var body = JObject.Parse(sqsMessge);

            var type = body["Subject"] ?? string.Empty;
            return type.ToString();
        }
    }
}
