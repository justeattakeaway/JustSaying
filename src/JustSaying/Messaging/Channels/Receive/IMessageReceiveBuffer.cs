using System.Threading.Channels;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging.Channels.Receive
{
    /// <summary>
    /// Provides a runnable, cancellable stream of messages to consumers.
    /// </summary>
    internal interface IMessageReceiveBuffer : IInterrogable
    {
        /// <summary>
        /// Starts listening for messages and exposes them via <see cref="Reader"/>.
        /// </summary>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> that will cancel the buffer and close the
        /// <see cref="ChannelReader{IQueueMessageContext}"/>.</param>
        /// <returns>A <see cref="Task"/> that either completes or throws an
        /// <see cref="System.OperationCanceledException"/> when the buffer is cancelled.</returns>
        Task RunAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Gets a channel reader that provides asynchronous access to received messages.
        /// </summary>
        ChannelReader<IQueueMessageContext> Reader { get; }

        string QueueName { get; }
    }
}
