using JustSaying.Models;

namespace JustSaying.Sample.Middleware.Messages;

public class UnreliableMessage : Message
{
    public string Name { get; set; } = Guid.NewGuid().ToString();
}
