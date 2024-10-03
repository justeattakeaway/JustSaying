using JustSaying.Models;
using Newtonsoft.Json;

namespace JustSaying.Messaging.MessageSerialization;

public sealed class NewtonsoftSerializationFactory(JsonSerializerSettings settings = null) : IMessageBodySerializationFactory
{
    public IMessageBodySerializer GetSerializer<T>() where T : Message => new NewtonsoftMessageBodySerializer<T>(settings);
}
