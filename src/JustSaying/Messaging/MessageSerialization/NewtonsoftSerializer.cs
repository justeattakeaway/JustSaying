using System;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JustSaying.Messaging.MessageSerialization
{
    public class NewtonsoftSerializer : IMessageSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public NewtonsoftSerializer()
            : this(null)
        {
        }

        public NewtonsoftSerializer(JsonSerializerSettings settings)
        {
            if (settings == null)
            {
                settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Converters = new JsonConverter[] { new Newtonsoft.Json.Converters.StringEnumConverter() }
                };
            }

            _settings = settings;
        }

        public Message Deserialize(string message, Type type)
        {
            var document = JObject.Parse(message);
            string json = document["Message"].ToString();

            return (Message)JsonConvert.DeserializeObject(json, type, _settings);
        }

        public string Serialize(Message message, bool serializeForSnsPublishing, string subject)
        {
            var json = JsonConvert.SerializeObject(message, _settings);

            // AWS SNS service will add Subject and Message properties automatically,
            // so just return plain message
            if (serializeForSnsPublishing)
            {
                return json;
            }

            // For direct publishing to SQS, add Subject and Message properties manually
            var context = new { Subject = subject, Message = json };
            return JsonConvert.SerializeObject(context, _settings);
        }

        public MessageAttributes GetMessageAttributes(string message)
        {
            var body = JObject.Parse(message);
            return new MessageAttributes(null );
        }

        public string GetMessageSubject(string sqsMessage)
        {
            var body = JObject.Parse(sqsMessage);
            return body.Value<string>("Subject") ?? string.Empty;
        }
    }
}
