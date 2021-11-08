using JustSaying.Messaging.Channels.Context;

namespace JustSaying.AwsTools.MessageHandling.Dispatch
{
    /// <summary>
    /// Dispatches messages to the queue.
    /// </summary>
    public interface IMessageDispatcher
    {
        /// <summary>
        /// Dispatches the message in <see cref="IQueueMessageContext"/> to the queue in the context.
        /// </summary>
        /// <param name="messageContext">A handle to the queue and message to dispatch.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to stop processing the message dispatch.</param>
        /// <returns>A <see cref="Task"/> that completes once the message has been dispatched.</returns>
        Task DispatchMessageAsync(IQueueMessageContext messageContext, CancellationToken cancellationToken);
    }
}
