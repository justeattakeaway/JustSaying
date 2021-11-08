using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.TestingFramework;

public class NoOpHandler<T> : IHandlerAsync<T> where T : Message
{
    public Task<bool> Handle(T message) => Task.FromResult(true);
}