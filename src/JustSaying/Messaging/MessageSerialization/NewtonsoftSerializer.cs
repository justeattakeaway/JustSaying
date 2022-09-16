using Amazon.SQS.Model;
using JustSaying.Messaging.MessageHandling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Message = JustSaying.Models.Message;

namespace JustSaying.Messaging.MessageSerialization;

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

    public Dictionary<string, MessageAttributeValue> GetMessageAttributes(string message)
    {
        var attributes = new Dictionary<string, MessageAttributeValue>();
        var props = JObject.Parse(message).Value<JObject>("MessageAttributes")?.Properties();

        if (props == null)
        {
            return attributes;
        }

        foreach (var prop in props)
        {
            var propData = prop.Value;

            var dataType = propData["Type"].ToString();
            var dataValue = propData["Value"].ToString();

            var isString = dataType == "String";

            var mav = new MessageAttributeValue
            {
                DataType = dataType,
                StringValue = isString ? dataValue : null,
                BinaryValue = !isString ? new MemoryStream(Convert.FromBase64String(dataValue), false) : null
            };
            attributes.Add(prop.Name, mav);
        }

        return attributes;
    }

    public string GetMessageSubject(string sqsMessage)
    {
        if (string.IsNullOrWhiteSpace(sqsMessage)) return string.Empty;

        var body = JObject.Parse(sqsMessage);
        return body.Value<string>("Subject") ?? string.Empty;
    }
}
