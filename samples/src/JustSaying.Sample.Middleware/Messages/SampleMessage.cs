using JustSaying.Models;

namespace JustSaying.Sample.Middleware.Messages;

public class SampleMessage : Message
{
    public string SampleId { get; set; } = Guid.NewGuid().ToString();
}
