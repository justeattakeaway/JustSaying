using Amazon.SQS.Model;
using CanaryDemo.Shared;
using JustSaying.Messaging.Middleware.Receive;
using Microsoft.Extensions.Logging;

namespace SampleApp;

/// <summary>
/// The cooperative alternative to pulsing the pause signal: the same PWM clock, but
/// enforced in the receive middleware, which sits in front of every ReceiveMessage call.
/// During the on-window polls pass through untouched (the pod is indistinguishable from
/// an unthrottled one); at the off-window boundary the *next* poll is simply not started
/// until the window reopens. A poll, once issued, always completes naturally — nothing is
/// ever cancelled, so no message is stranded invisible mid-delivery ("casualties").
/// The cost: the final poll of a window lingers up to the receive wait time into the
/// off-window, so the receive wait must be well under the PWM period (1s wait / 10s
/// period validated). Works on any JustSaying version — no dependency on the 8.1.1
/// pause-cancellation behaviour.
/// </summary>
public sealed class GatedReceiveMiddleware(PwmGate gate, ILogger<DefaultReceiveMessagesMiddleware> logger)
    : DefaultReceiveMessagesMiddleware(logger)
{
    protected override async Task<IList<Message>> RunInnerAsync(
        ReceiveMessagesContext context,
        Func<CancellationToken, Task<IList<Message>>> func,
        CancellationToken stoppingToken)
    {
        try
        {
            await gate.WaitForOnWindowAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Shutdown, or the receive-buffer read timeout (5 min default) elapsed while
            // parked at weight 0. Return empty like the base middleware does on
            // cancellation; the receive loop just tries again.
            return [];
        }

        return await base.RunInnerAsync(context, func, stoppingToken).ConfigureAwait(false);
    }
}
