using JustSaying.Messaging;

namespace JustSaying.Fluent;

internal interface ITopicPublisher
{
    Func<CancellationToken, Task> StartupTask { get; }
    MessagePublisher Publisher { get; }
}
