using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            var props = JObject.Parse(message).Value<JObject>("MessageAttributes")?.Properties();
            if (props == null) return new MessageAttributes();
            var dict = new Dictionary<string, MessageAttributeValue>();

            foreach (var prop in props)
            {
                var propData = prop.Value as JObject;
                if (propData == null) continue;

                var dataType = propData.GetValue("Type", StringComparison.Ordinal);
                if (dataType == null) continue;

                var isString = dataType.ToString().Equals("String", StringComparison.Ordinal);
                var data = propData.GetValue("Value", StringComparison.Ordinal);

                var mav = new MessageAttributeValue
                {
                    DataType = dataType.ToString(),
                    StringValue = isString ? data.ToString() : null,
                    BinaryValue = !isString ? Convert.FromBase64String(data.ToString()) : null
                };
                dict.Add(prop.Name, mav);
            }

            return new MessageAttributes(dict);
        }

        public string GetMessageSubject(string sqsMessage)
        {
            var body = JObject.Parse(sqsMessage);
            return body.Value<string>("Subject") ?? string.Empty;
        }
    }
}
