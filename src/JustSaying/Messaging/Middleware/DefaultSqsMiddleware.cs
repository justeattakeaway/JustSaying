using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.Messaging.Channels;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Middleware
{
    public class DefaultSqsMiddleware : MiddlewareBase<GetMessagesContext, IList<Amazon.SQS.Model.Message>>
    {
        private readonly ILogger<DefaultSqsMiddleware> _logger;

        public DefaultSqsMiddleware(ILogger<DefaultSqsMiddleware> logger) : base(null)
        {
            _logger = logger;
        }

        protected override async Task<IList<Message>> RunInnerAsync(GetMessagesContext context, Func<Task<IList<Message>>> func)
        {
            try
            {
                var results = await func().ConfigureAwait(false);

                _logger.LogTrace(
                    "Polled for messages on queue '{QueueName}' in region '{Region}', and received {MessageCount} messages.",
                    context.QueueName,
                    context.RegionName,
                    context.Count);

                return results;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogTrace(
                    ex,
                    "Could not determine number of messages to read from queue '{QueueName}' in '{Region}'.",
                    context.QueueName,
                    context.RegionName);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogTrace(
                    ex,
                    "Suspected no message on queue '{QueueName}' in region '{Region}'.",
                    context.QueueName,
                    context.RegionName);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(
                    ex,
                    "Error receiving messages on queue '{QueueName}' in region '{Region}'.",
                    context.QueueName,
                    context.RegionName);
            }

            return Array.Empty<Message>();
        }
    }
}
