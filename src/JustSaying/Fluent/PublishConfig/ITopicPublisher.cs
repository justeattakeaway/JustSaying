using JustSaying.Messaging;

namespace JustSaying.Fluent;

internal interface ITopicPublisher
{
    public Func<CancellationToken, Task> StartupTask { get; }
    public IMessagePublisher Publisher { get; }
}
