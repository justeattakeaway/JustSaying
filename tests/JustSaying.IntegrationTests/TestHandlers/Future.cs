using JustSaying.Models;
using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests.TestHandlers;

public class Future<TMessage>(Func<Task> action)
    where TMessage : Message
{
    private readonly TaskCompletionSource<object> _doneSignal = new();
    private readonly Func<Task> _action = action;
    private readonly List<TMessage> _messages = new();

    public Future()
        : this(null)
    {
    }

    public async Task Complete(TMessage message)
    {
        try
        {
            _messages.Add(message);

            if (_action != null)
            {
                await _action();
            }
        }
        finally
        {
            if (ReceivedMessageCount >= ExpectedMessageCount)
            {
                TaskHelpers.DelaySendDone(_doneSignal);
            }
        }
    }

    public Task DoneSignal => _doneSignal.Task;

    public int ExpectedMessageCount { get; set; } = 1;

    public int ReceivedMessageCount => _messages.Count;

    public Exception RecordedException { get; set; }

    public bool HasReceived(TMessage message)
    {
        return _messages.Any(m => m.Id == message.Id);
    }
}