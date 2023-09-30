using JustSaying.Messaging.Middleware;
using Xunit.Abstractions;

namespace JustSaying.TestingFramework;

public class AwaitableMiddleware(ITestOutputHelper outputHelper) : MiddlewareBase<HandleMessageContext, bool>
{
    private readonly ITestOutputHelper _outputHelper = outputHelper;
    public Task Complete { get; private set; }

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
