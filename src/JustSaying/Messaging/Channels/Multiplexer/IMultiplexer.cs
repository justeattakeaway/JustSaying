using System.Threading.Channels;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Interrogation;

namespace JustSaying.Messaging.Channels.Multiplexer
{
    /// <summary>
    /// Reduces multiple input streams of messages into a single stream, interleaving them.
    /// </summary>
    internal interface IMultiplexer : IInterrogable
    {
        /// <summary>
        /// Begins reading from instances of <see cref="ChannelReader{IQueueMessageContext}"/> passed in from <see cref="ReadFrom"/>
        /// and makes the interleaved result available from <see cref="GetMessagesAsync"/>.
        /// </summary>
        /// <param name="stoppingToken">Cancels the multiplexer and closes the
        /// <see cref="IAsyncEnumerable{IQueueMessageContext}"/> returned from <see cref="GetMessagesAsync"/>.</param>
        /// <returns>A <see cref="Task"/> that completes or throws when the multiplexer finishes.</returns>
        Task RunAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Adds a <see cref="ChannelReader{IQueueMessageContext}" /> to be multiplexed into the stream.
        /// </summary>
        /// <param name="reader">A channel reader of messages.</param>
        void ReadFrom(ChannelReader<IQueueMessageContext> reader);

        /// <summary>
        /// Provides a multiplexed stream of messages.
        /// </summary>
        /// <returns>An <see cref="IAsyncEnumerable{IQueueMessageContext}"/> of messages.</returns>
        IAsyncEnumerable<IQueueMessageContext> GetMessagesAsync();
    }
}
