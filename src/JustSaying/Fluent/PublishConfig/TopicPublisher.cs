using JustSaying.Messaging;

namespace JustSaying.Fluent;

public interface TopicPublisher
{
    public Func<CancellationToken, Task> StartupTask { get; }
    public IMessagePublisher Publisher { get; }
}
