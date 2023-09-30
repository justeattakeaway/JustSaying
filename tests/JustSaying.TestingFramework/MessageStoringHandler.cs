using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;

namespace JustSaying.TestingFramework;

public interface IMessageStore<TMessage>
{
    IList<TMessage> Messages { get; }
    Guid Id { get; }
}

public class TestMessageStore<TMessage> : IMessageStore<TMessage>
{
    public Guid Id { get; }
    public IList<TMessage> Messages { get; }

    public TestMessageStore(ILogger<TestMessageStore<TMessage>> logger)
    {
        Id = Guid.NewGuid();
        Messages = new List<TMessage>();
        logger.LogInformation("Creating TestMessageStore with id {Id}", Id);
    }
}

public class MessageStoringHandler<T>(IMessageStore<T> store, ILogger<MessageStoringHandler<T>> logger) : IHandlerAsync<T>
{
    private readonly ILogger<MessageStoringHandler<T>> _logger = logger;
    public IMessageStore<T> MessageStore { get; } = store;

    public Task<bool> Handle(T message)
    {
        _logger.LogInformation("Handling message type {T} with store id {StoreId}", typeof(T).Name, MessageStore.Id );

        MessageStore.Messages.Add(message);
        return Task.FromResult(true);
    }
}