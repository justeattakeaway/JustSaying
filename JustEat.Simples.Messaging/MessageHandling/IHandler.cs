namespace JustEat.Simples.NotificationStack.Messaging.MessageHandling
{
    /// <summary>
    /// Message handlers
    /// </summary>
    /// <typeparam name="T">Type of message to be handled</typeparam>
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