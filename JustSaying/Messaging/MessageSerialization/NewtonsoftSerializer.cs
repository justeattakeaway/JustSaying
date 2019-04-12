using System;
using JustSaying.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JustSaying.Messaging.MessageSerialization
{
    public class NewtonsoftSerializer : IMessageSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public NewtonsoftSerializer()
        {
            _settings = null;
        }

        public NewtonsoftSerializer(JsonSerializerSettings settings) : this()
        {
            _settings = settings;
        }

        public Message Deserialize(string message, Type type)
        {
            var jsqsMessage = JObject.Parse(message);
            var messageBody = jsqsMessage["Message"].ToString();
            return (Message)JsonConvert.DeserializeObject(messageBody, type, GetJsonSettings());
        }

        public string Serialize(Message message, bool serializeForSnsPublishing, string subject)
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
                Converters = new JsonConverter[] { new Newtonsoft.Json.Converters.StringEnumConverter() }
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
