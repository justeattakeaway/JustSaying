using Newtonsoft.Json;

namespace JustSaying.Messaging.Interrogation
{
    internal class InterrogationResultJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null && value.GetType() != typeof(InterrogationResult))
            {
                throw new InvalidOperationException(
                    $"This converter can only be used with {nameof(InterrogationResult)}");
            }

            serializer.Serialize(writer, (value as InterrogationResult)?.Data);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(InterrogationResult);
        }
    }
}
