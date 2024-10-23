using System.Collections.Concurrent;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.TestingFramework;

public class InspectableHandler<T> : IHandlerAsync<T>
{
    public InspectableHandler()
    {
        ReceivedMessages = [];
        ShouldSucceed = true;
    }

    public Action<T> OnHandle { get; set; }
    public ConcurrentQueue<T> ReceivedMessages { get; }

    public bool ShouldSucceed { get; set; }

    public virtual Task<bool> Handle(T message)
    {
        ReceivedMessages.Enqueue(message);

        OnHandle?.Invoke(message);

        return Task.FromResult(ShouldSucceed);
    }
}
