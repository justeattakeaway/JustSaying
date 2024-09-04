using System.Text.RegularExpressions;
using JustSaying.Models;
using Newtonsoft.Json;

namespace JustSaying.Messaging.MessageSerialization;

public class NewtonsoftMessageBodySerializer<T> : IMessageBodySerializer where T: Message
{
    private readonly JsonSerializerSettings _settings;

    public NewtonsoftMessageBodySerializer()
    {
        _settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = [new Newtonsoft.Json.Converters.StringEnumConverter()]
        };
    }

    public NewtonsoftMessageBodySerializer(JsonSerializerSettings settings)
    {
        _settings = settings;
    }

    public string Serialize(Message message)
    {
        return JsonConvert.SerializeObject(message, _settings);
    }

    public Message Deserialize(string message)
    {
        return JsonConvert.DeserializeObject<T>(message, _settings);
    }
}
