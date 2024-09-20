using System.Text.Json;

namespace JustSaying.Sample.Restaurant.OrderingApi;

public class MessagingJsonSerializerOptions
{
    public JsonSerializerOptions SerializerOptions { get; } = new();
}
