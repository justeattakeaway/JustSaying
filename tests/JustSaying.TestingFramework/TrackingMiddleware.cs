using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Handle;

namespace JustSaying.TestingFramework
{
    public class TrackingMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        private readonly Action<string> _onBefore;
        private readonly Action<string> _onAfter;
        private readonly string _id;

        public TrackingMiddleware(string id, Action<string> onBefore, Action<string> onAfter)
        {
            _id = id;
            _onBefore = onBefore;
            _onAfter = onAfter;
        }

        protected override async Task<bool> RunInnerAsync(
            HandleMessageContext context,
            Func<CancellationToken,
                Task<bool>> func,
            CancellationToken stoppingToken)
        {
            _onBefore(_id);
            var result = await func(stoppingToken).ConfigureAwait(false);
            _onAfter(_id);

            return result;
        }
    }
}
