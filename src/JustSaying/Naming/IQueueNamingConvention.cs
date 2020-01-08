namespace JustSaying.Naming
{
    /// <summary>
    /// Defines a method for creating a queue name.
    /// </summary>
    public interface IQueueNamingConvention
    {
        /// <summary>
        /// Returns the queue name to use.
        /// </summary>
        /// <typeparam name="T">
        /// The message type
        /// </typeparam>
        /// <returns>The queue name that will be used for the message type.</returns>
        string QueueName<T>();
    }
}
