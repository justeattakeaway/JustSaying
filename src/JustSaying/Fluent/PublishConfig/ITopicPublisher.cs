using JustSaying.Messaging;

namespace JustSaying.Fluent;

internal interface ITopicPublisher<in TMessage> where TMessage : class
{
    Func<CancellationToken, Task> StartupTask { get; }
    IMessagePublisher<TMessage> Publisher { get; }
}
