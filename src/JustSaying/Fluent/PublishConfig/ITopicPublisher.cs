using JustSaying.Messaging;

namespace JustSaying.Fluent;

internal interface ITopicPublisher<in T> where T : class
{
    Func<CancellationToken, Task> StartupTask { get; }
    IMessagePublisher<T> Publisher { get; }
}
