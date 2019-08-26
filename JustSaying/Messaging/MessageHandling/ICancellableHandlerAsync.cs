using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageHandling
{
    /// <summary>
    /// An asynchronous message handler that supports cancellation.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the message to handle.
    /// </typeparam>
    public interface ICancellableHandlerAsync<in T> : IHandlerAsync<T>
    {
        /// <summary>
        /// Handles a message of type <typeparamref name="T"/> as an asynchronous operation.
        /// </summary>
        /// <param name="message">
        /// The message to handle.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which is cancelled when the message visibility timeout expires.
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation
        /// to process the message that returns <see langword="true"/> if the message
        /// was processed successfully; otherwise <see langword="false"/>.
        /// </returns>
        Task<bool> HandleAsync(T message, CancellationToken cancellationToken);
    }
}
