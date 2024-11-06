namespace JustSaying.Messaging.MessageSerialization;

internal interface IMessageAndAttributesDeserializer : IMessageSerializer
{
    MessageWithAttributes DeserializeWithAttributes(string message, Type type);
}
