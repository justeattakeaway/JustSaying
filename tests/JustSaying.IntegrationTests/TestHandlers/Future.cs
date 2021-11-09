using JustSaying.Models;
using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests.TestHandlers;

public class Future<TMessage>
    where TMessage : Message
{
    private readonly TaskCompletionSource<object> _doneSignal = new TaskCompletionSource<object>();
    private readonly Func<Task> _action;
    private readonly List<TMessage> _messages = new List<TMessage>();

    public Future()
        : this(null)
    {
    }

    public Future(Func<Task> action)
    {
        _action = action;
        ExpectedMessageCount = 1;
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

    public int ExpectedMessageCount { get; set; }

    public int ReceivedMessageCount => _messages.Count;

    public Exception RecordedException { get; set; }

    public bool HasReceived(TMessage message)
    {
        return _messages.Any(m => m.Id == message.Id);
    }
}