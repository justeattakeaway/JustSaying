using JustSaying.Messaging.Middleware;
using Xunit.Abstractions;

namespace JustSaying.TestingFramework;

public class AwaitableMiddleware : MiddlewareBase<HandleMessageContext, bool>
{
    private readonly ITestOutputHelper _outputHelper;
    public Task Complete { get; private set; }

    public AwaitableMiddleware(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        var tcs = new TaskCompletionSource();
        Complete = tcs.Task;
        try
        {
            return await func(stoppingToken);
        }
        finally
        {
            _outputHelper.WriteLine("Completing AwaitableMiddleware - the job is done.");
            tcs.SetResult();
        }
    }
}
