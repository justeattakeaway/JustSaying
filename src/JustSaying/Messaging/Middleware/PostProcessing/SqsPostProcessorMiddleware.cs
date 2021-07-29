using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Middleware.PostProcessing
{
    public class LoggingMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
        {
            _logger = logger;
        }

        private const string MessageTemplate = "{Status} handling message with Id '{MessageId}' of type {MessageType} in {TimeToHandle}ms.";
        private const string Succeeded = nameof(Succeeded);
        private const string Failed = nameof(Failed);

        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            using var disposable = _logger.BeginScope(new Dictionary<string, object>()
            {
                ["MessageSource"] = context.QueueName,
                ["SourceType"] = "Queue"
            });

            var watch = Stopwatch.StartNew();
            bool dispatchSuccessful = false;
            try
            {
                dispatchSuccessful = await func(stoppingToken);
                watch.Stop();
                return dispatchSuccessful;
            }
            finally
            {
                if (dispatchSuccessful)
                {
                    _logger.LogInformation(MessageTemplate,
                        "Succeeded",
                        context.Message.Id,
                        context.MessageType.Name,
                        watch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning(MessageTemplate,
                        "Failed",
                        context.Message.Id,
                        context.MessageType.Name,
                        watch.ElapsedMilliseconds);
                }
            }
        }
    }

    public class SqsPostProcessorMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        private readonly ILogger<SqsPostProcessorMiddleware> _logger;

        public SqsPostProcessorMiddleware(ILogger<SqsPostProcessorMiddleware> logger)
        {
            _logger = logger;
        }

        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            try
            {
                var succeeded = await func(stoppingToken);

                if (succeeded)
                {
                    await context.MessageDeleter.DeleteMessage(stoppingToken);
                }

                return succeeded;
            }
            finally
            { }
        }
    }
}
