using JustSaying.Messaging;

namespace JustSaying.Fluent;

internal interface ITopicPublisher
{
    Func<CancellationToken, Task> StartupTask { get; }
    IMessagePublisher Publisher { get; }
    IMessageBatchPublisher BatchPublisher { get; }
}
