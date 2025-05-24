using JustSaying.Messaging.Middleware;
using Xunit.Abstractions;

namespace JustSaying.TestingFramework;

public class AwaitableMiddleware(ITestOutputHelper outputHelper, int runCountToAwait = 1) : MiddlewareBase<HandleMessageContext, bool>
{
    private readonly ITestOutputHelper _outputHelper = outputHelper;
    private int _runCountToAwait = runCountToAwait;
    private readonly TaskCompletionSource _tcs = new (TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Complete => _tcs.Task;

    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        try
        {
            return await func(stoppingToken);
        }
        finally
        {
            Interlocked.Decrement(ref _runCountToAwait);
            if (_runCountToAwait == 0)
            {
                _outputHelper.WriteLine("Completing AwaitableMiddleware - the job is done.");
                _tcs.SetResult();
            }
        }
    }
}
