using System;

namespace JustSaying.Messaging.MessageHandling
{
    /// <summary>
    /// Synchronous message handler
    /// </summary>
    /// <typeparam name="T">Type of message to be handled</typeparam>
    [Obsolete("replaced by IAsyncHandler<T>")]
    public interface IHandler<in T>
    {
        /// <summary>
        /// Handle a message of a given type
        /// </summary>
        /// <param name="message">Message to handle</param>
        /// <returns>Was handling successful?</returns>
        bool Handle(T message);
    }
}