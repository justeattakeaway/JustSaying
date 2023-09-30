using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class RecordingMessageContextAccessor(IMessageContextAccessor inner) : IMessageContextAccessor, IMessageContextReader
{
    private readonly IMessageContextAccessor _inner = inner;
    private readonly List<MessageContext> _valuesWritten = new();

    public IReadOnlyCollection<MessageContext> ValuesWritten => _valuesWritten;

    public MessageContext MessageContext
    {
        get => _inner.MessageContext;
        set
        {
            if (value != null)
            {
                _valuesWritten.Add(value);
            }

            _inner.MessageContext = value;
        }
    }
}