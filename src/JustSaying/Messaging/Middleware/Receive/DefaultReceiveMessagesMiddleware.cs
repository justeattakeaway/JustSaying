using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Middleware.Receive;

/// <summary>
/// The default middleware to use for the receive pipeline.
/// </summary>
/// <remarks>
/// Creates an instance of <see cref="DefaultReceiveMessagesMiddleware"/>.
/// </remarks>
/// <param name="logger">The <see cref="ILogger"/> to use.</param>
public class DefaultReceiveMessagesMiddleware(ILogger<DefaultReceiveMessagesMiddleware> logger) : MiddlewareBase<ReceiveMessagesContext, IList<Message>>
{
    protected override async Task<IList<Message>> RunInnerAsync(
        ReceiveMessagesContext context,
        Func<CancellationToken, Task<IList<Message>>> func,
        CancellationToken stoppingToken)
    {
        try
        {
            var results = await func(stoppingToken).ConfigureAwait(false);

            logger.LogTrace(
                "Polled for messages on queue '{QueueName}' in region '{Region}', and received {MessageCount} messages.",
                context.QueueName,
                context.RegionName,
                results.Count);

            return results;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogTrace(
                ex,
                "Request to get more messages from queue was canceled for queue '{QueueName}' in region '{Region}'," +
                "likely because there are no messages in the queue. " +
                "This might have also been caused by the application shutting down.",
                context.QueueName,
                context.RegionName);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error receiving messages on queue '{QueueName}' in region '{Region}'.",
                context.QueueName,
                context.RegionName);
        }

        return Array.Empty<Message>();
    }
}
